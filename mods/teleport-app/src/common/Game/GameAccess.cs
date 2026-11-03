using System.Collections.Generic;

namespace TeleportApp
{
    /// <summary>One place the player can go: a property, a business or a dealer, plus the text the list shows.</summary>
    internal sealed class Place
    {
        public PlaceKind Kind;
        public GameProperty Property;   // properties and businesses
        public GameDealer Dealer;       // dealers
        public Vector3 Point;           // stores
        public GameVehicle Vehicle;     // vehicles
        public string Code;
        public string Name;
        public bool Held = true;        // owned (property, business) or hired (dealer)

        /// <summary>The text shown in lists: the name, marked when the place is not owned or not hired.</summary>
        public string Label => Held ? Name : Name + (Kind == PlaceKind.Dealer ? "  (not hired)" : "  (not owned)");
    }

    /// <summary>
    /// Every read of or call into the game goes through here, so the rest of the mod is identical for
    /// both runtimes. Nothing is cached: lists are read each time because ownership changes while playing.
    /// </summary>
    internal static class GameAccess
    {
        // Spawn points sit on the ground and some a touch below it; this lifts the landing a little.
        private const float Clearance = 0.3f;

        // How far in front of a dealer the player lands, so they do not stand inside the dealer.
        private const float DealerDistance = 1.5f;

        // How far past the side of a vehicle the player lands.
        private const float VehicleGap = 1.5f;

        /// <summary>
        /// Everything the player can travel to, as the settings allow: properties, businesses and dealers,
        /// in that order and each by name (see <see cref="Destinations"/>). Owned state is read live.
        /// </summary>
        public static List<Place> Places()
        {
            var businessCodes = new HashSet<string>();
            var businesses = GameBusiness.Businesses;
            for (int i = 0; businesses != null && i < businesses.Count; i++)
                if (businesses[i] != null) businessCodes.Add(businesses[i].PropertyCode);

            var all = new List<Place>();

            // Businesses are properties too, so one pass over the property list covers both kinds.
            var properties = GameProperty.Properties;
            for (int i = 0; properties != null && i < properties.Count; i++)
            {
                GameProperty p = properties[i];
                if (p == null) continue;

                bool isBusiness = businessCodes.Contains(p.PropertyCode);
                bool offered = isBusiness
                    ? Filter.Offers(ModConfig.ShowBusinesses, ModConfig.OnlyOwnedBusinesses, p.IsOwned)
                    : Filter.Offers(ModConfig.ShowProperties, ModConfig.OnlyOwnedProperties, p.IsOwned);
                if (offered) all.Add(MakeProperty(p, isBusiness ? PlaceKind.Business : PlaceKind.Property));
            }

            // The game's Dealer Management app lists the hired ones from this same list.
            var dealers = GameDealer.AllPlayerDealers;
            for (int i = 0; dealers != null && i < dealers.Count; i++)
            {
                GameDealer d = dealers[i];
                if (d == null || !Filter.Offers(ModConfig.ShowDealers, ModConfig.OnlyHiredDealers, d.IsRecruited)) continue;
                all.Add(new Place { Kind = PlaceKind.Dealer, Dealer = d, Code = d.ID, Name = d.FullName, Held = d.IsRecruited });
            }

            if (Filter.Offers(ModConfig.ShowStores, false, true))
                foreach (Stores.Store store in Stores.All)
                    all.Add(new Place { Kind = PlaceKind.Store, Point = new Vector3(store.X, store.Y, store.Z), Code = "store:" + store.Name, Name = store.Name });

            if (ModConfig.ShowVehicles && NetworkSingleton<GameVehicleManager>.InstanceExists)
            {
                // Read from the list of every vehicle: the owned list the game keeps is only filled on the host.
                var vehicles = NetworkSingleton<GameVehicleManager>.Instance.AllVehicles;
                for (int i = 0; vehicles != null && i < vehicles.Count; i++)
                {
                    GameVehicle v = vehicles[i];
                    if (v == null || !v.IsPlayerOwned) continue;
                    all.Add(new Place { Kind = PlaceKind.Vehicle, Vehicle = v, Code = v.GUID.ToString(), Name = VehicleName(v) });
                }
            }

            return Destinations.Order(all, x => x.Code, x => x.Name, x => (int)x.Kind);
        }

        /// <summary>The places the hotkeys step through: the listed places that are also in the rotation.</summary>
        public static List<Place> RotationPlaces()
            => Places().FindAll(p => ModConfig.InRotation(p.Kind, p.Code));

        // "Red Shitbox": the colour tells apart two of the same model.
        private static string VehicleName(GameVehicle v)
        {
            string color = Singleton<GameVehicleColors>.InstanceExists ? Singleton<GameVehicleColors>.Instance.GetColorName(v.OwnedColor) : "";
            return string.IsNullOrEmpty(color) ? v.VehicleName : color + " " + v.VehicleName;
        }

        private static Place MakeProperty(GameProperty property, PlaceKind kind) => new Place
        {
            Kind = kind,
            Property = property,
            Code = property.PropertyCode,
            Name = property.PropertyName,
            Held = property.IsOwned
        };

        /// <summary>The chosen spawn point of a property; the other one when the chosen one is missing.</summary>
        private static Transform SpawnOf(GameProperty property, bool interior)
        {
            Transform first = interior ? property.InteriorSpawnPoint : property.SpawnPoint;
            Transform second = interior ? property.SpawnPoint : property.InteriorSpawnPoint;
            return first != null ? first : second;
        }

        /// <summary>
        /// Puts a store's point on the ground just below it. The stored heights are about the height of a
        /// standing player (the ground plus roughly a metre), so the look starts half a metre above the
        /// point: high enough to clear the ground, low enough to start under roofs and awnings, which an
        /// earlier, higher start landed the player on. When nothing flat is found the point is used as given.
        /// </summary>
        private static Vector3 GroundAt(Vector3 point)
        {
            RaycastHit hit;
            // Everything except the ignore-raycast layer, and not triggers (as the game's own checks do).
            if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out hit, 4f, -5, QueryTriggerInteraction.Ignore)
                && hit.normal.y >= 0.5f)
                return hit.point;
            return point;
        }

        /// <summary>
        /// Where the player lands for a place. A dealer moves, so this is read at the moment of the
        /// teleport: in front of the dealer, facing them. A dealer indoors stands at the building's door.
        /// </summary>
        private static bool TryGetTarget(Place place, bool interior, Quaternion current, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (place.Kind == PlaceKind.Store)
            {
                position = GroundAt(place.Point);
                rotation = current;   // a store has no facing of its own: keep the way the player looks
                return true;
            }

            if (place.Kind == PlaceKind.Vehicle)
            {
                // The player lands beside the vehicle (a metre and a half past its side), facing it.
                if (place.Vehicle == null || place.Vehicle.transform.position.y < -50f) return false;
                Transform car = place.Vehicle.transform;
                float halfWidth = place.Vehicle.boundingBox != null ? place.Vehicle.BoundingBoxDimensions.x / 2f : 1f;
                Vector3 side = Vector3.ProjectOnPlane(car.right, Vector3.up).normalized;
                position = GroundAt(car.position + side * (halfWidth + VehicleGap));
                rotation = Quaternion.LookRotation(-side);
                return true;
            }

            if (place.Kind == PlaceKind.Dealer)
            {
                if (place.Dealer == null) return false;
                Transform npc = place.Dealer.transform;
                Vector3 forward = Vector3.ProjectOnPlane(npc.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                forward.Normalize();
                position = npc.position + forward * DealerDistance;
                rotation = Quaternion.LookRotation(-forward);
                return true;
            }

            if (place.Property == null) return false;
            Transform spawn = SpawnOf(place.Property, interior);
            if (spawn == null) return false;
            position = spawn.position;
            rotation = spawn.rotation;
            return true;
        }

        /// <summary>True while a key press should go to the game, not to the mod (typing, paused, the phone open).</summary>
        public static bool InputBlocked()
        {
            if (GameInput.IsTyping) return true;
            // The phone has its own way to pick a place; the keys would teleport behind its list.
            if (Singleton<GameplayMenu>.InstanceExists && Singleton<GameplayMenu>.Instance.IsOpen) return true;
            return Singleton<PauseMenu>.InstanceExists && Singleton<PauseMenu>.Instance.IsPaused;
        }

        /// <summary>Moves the local player to the place. Returns the reason it did not, or an empty text.</summary>
        public static string TeleportTo(Place place, bool interior, out Vector3 target)
        {
            target = Vector3.zero;
            Player player = Player.Local;
            bool ready = player != null && PlayerSingleton<PlayerMovement>.InstanceExists;
            Quaternion rotation = Quaternion.identity;
            bool hasTarget = ready && TryGetTarget(place, interior, player.transform.rotation, out target, out rotation);

            Refusal refusal = Guard.Check(ready, ready && player.IsInVehicle, hasTarget);
            if (refusal != Refusal.None) return Guard.Describe(refusal);

            target += Vector3.up * Clearance;
            ModLog.Verbose("Teleporting to " + place.Name + " [" + place.Code + "]: player at " + player.transform.position + ", target " + target + ".");

            // Only the local player moves; the game shares the new position with the other players.
            // The player's position is the middle of its body, so the feet are aligned to the point.
            PlayerSingleton<PlayerMovement>.Instance.Teleport(target, rotation, true);
            return "";
        }
    }
}
