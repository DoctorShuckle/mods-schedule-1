using System.Collections.Generic;

namespace TeleportToProperties
{
    /// <summary>One place the player can go: the game's property plus the text the list shows.</summary>
    internal sealed class Place
    {
        public GameProperty Property;
        public string Code;
        public string Name;
        public bool IsBusiness;
    }

    /// <summary>
    /// Every read of or call into the game goes through here, so the rest of the mod is identical for
    /// both runtimes. Nothing is cached: lists are read each time because ownership changes while playing.
    /// </summary>
    internal static class GameAccess
    {
        // Spawn points sit on the ground and some a touch below it; this lifts the landing a little.
        private const float Clearance = 0.3f;

        /// <summary>The properties and businesses the player owns, in list order (see <see cref="Destinations"/>).</summary>
        public static List<Place> OwnedPlaces()
        {
            var business = new HashSet<string>();
            var all = new List<Place>();

            // Owned businesses are listed on their own so they can be told from properties.
            var businesses = GameBusiness.OwnedBusinesses;
            for (int i = 0; businesses != null && i < businesses.Count; i++)
            {
                GameBusiness b = businesses[i];
                if (b == null) continue;
                business.Add(b.PropertyCode);
                all.Add(Make(b, true));
            }

            var properties = GameProperty.OwnedProperties;
            for (int i = 0; properties != null && i < properties.Count; i++)
            {
                GameProperty p = properties[i];
                if (p == null || !p.IsOwned) continue;
                all.Add(Make(p, business.Contains(p.PropertyCode)));
            }

            return Destinations.Order(all, x => x.Code, x => x.Name, x => x.IsBusiness);
        }

        private static Place Make(GameProperty property, bool isBusiness) => new Place
        {
            Property = property,
            Code = property.PropertyCode,
            Name = property.PropertyName,
            IsBusiness = isBusiness
        };

        /// <summary>The chosen spawn point of a place; the other one when the chosen one is missing.</summary>
        public static Transform SpawnOf(Place place, bool interior)
        {
            Transform first = interior ? place.Property.InteriorSpawnPoint : place.Property.SpawnPoint;
            Transform second = interior ? place.Property.SpawnPoint : place.Property.InteriorSpawnPoint;
            return first != null ? first : second;
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
            Transform spawn = ready ? SpawnOf(place, interior) : null;

            Refusal refusal = Guard.Check(ready, ready && player.IsInVehicle, spawn != null);
            if (refusal != Refusal.None) return Guard.Describe(refusal);

            target = spawn.position + Vector3.up * Clearance;
            ModLog.Verbose("Teleporting " + place.Name + " [" + place.Code + "]: player at " + player.transform.position
                + ", spawn point \"" + spawn.name + "\" at " + target + ".");

            // Only the local player moves; the game shares the new position with the other players.
            // The player's position is the middle of its body, so the feet are aligned to the point.
            PlayerSingleton<PlayerMovement>.Instance.Teleport(target, spawn.rotation, true);
            return "";
        }
    }
}
