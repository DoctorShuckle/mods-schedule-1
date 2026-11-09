using System;
using System.Collections.Generic;
using HarmonyLib;

namespace BrickMixer
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
        public const string ItemId = "brickmixer";
        private const string ItemName = "Brick Mixer";

        private static BuildableItemDefinition _definition;
        private static string _mk2Id;
        private static float _mk2Price;
        private static PackagingDefinition _brick;
        private static bool _warnedMissing;

#if IL2CPP
        // IL2CPP proxies are wrappers: a reference typed as a base class or an interface must be
        // asked with TryCast, and compared by native pointer.
        private static T As<T>(object value) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
            => (value as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)?.TryCast<T>();

        private static bool Same(object a, object b)
            => a != null && b != null
               && ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)a).Pointer
               == ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)b).Pointer;
#else
        private static T As<T>(object value) where T : class => value as T;

        private static bool Same(object a, object b) => ReferenceEquals(a, b);

#endif

        /// <summary>True on the host (or in single player), the only machine that packs.</summary>
        public static bool IsServer => InstanceFinder.IsServer;

        // ------------------------------------------------------------------------------------
        // The item
        // ------------------------------------------------------------------------------------

        /// <summary>
        /// Makes the Brick Mixer exist and be in the item registry. The game empties its registry of runtime
        /// items whenever a scene changes, so this runs again at each game start. Returns false when the Mk2 it
        /// is copied from cannot be found.
        /// </summary>
        public static bool EnsureRegistered()
        {
            Registry registry = Singleton<Registry>.Instance;
            if (registry == null) return false;

            if (_definition == null && !Build(registry)) return false;
            if (!Registry.ItemExists(ItemId))
            {
                registry.AddToRegistry(_definition);
                ModLog.Verbose("Registered the Brick Mixer.");
            }
            return true;
        }

        private static bool Build(Registry registry)
        {
            BuildableItemDefinition source = FindMk2(registry);
            if (source == null)
            {
                if (!_warnedMissing)
                {
                    _warnedMissing = true;
                    ModLog.Warning("The Mixing Station Mk2 was not found in the item registry, so the Brick Mixer cannot be added.");
                }
                return false;
            }

            _mk2Id = source.ID;
            _mk2Price = source.BasePurchasePrice;

            // A station is built by instantiating BuiltItem. The copy lives under an inactive object, so its
            // Awake does not run; what the game instantiates from it is active and starts normally.
            GameObject holder = new GameObject("BrickMixerPrefabs");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);
            holder.hideFlags = HideFlags.HideAndDontSave;

            GameObject prefab = UnityEngine.Object.Instantiate(source.BuiltItem.gameObject, holder.transform);
            prefab.name = "BrickMixer";
            MixingStationMk2 station = prefab.GetComponent<MixingStationMk2>();
            if (station == null)
            {
                ModLog.Warning("The copied Mk2 prefab has no MixingStationMk2; the Brick Mixer cannot be added.");
                UnityEngine.Object.Destroy(holder);
                return false;
            }
            station.MaxMixQuantity = BrickRules.BrickUnits;
            station.RequiresIngredientInsertion = false;
            TintModel(prefab);

            BuildableItemDefinition definition = UnityEngine.Object.Instantiate(source);
            definition.name = "BrickMixer";
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.ID = ItemId;
            definition.Name = ItemName;
            definition.Description = "A mixing station for bricks. It takes one brick of product and 20 mixers, mixes them in one go, and gives the result back as a brick.";
            definition.BuiltItem = station;
            _definition = definition;
            RefreshPrice();
            ModLog.Verbose("Built the Brick Mixer from " + _mk2Id + ".");
            return true;
        }

        /// <summary>
        /// Gives the copy its own materials, tinted by the settings, so it stands out from a Mk2. Only meshes
        /// are touched, and only opaque materials (glass and glows are left alone). The Mk2's own materials
        /// are not changed.
        /// </summary>
        private static void TintModel(GameObject root)
        {
            float strength = ModConfig.TintStrength;
            if (strength <= 0f) return;

            float r = TintRules.Factor(ModConfig.TintRed, strength);
            float g = TintRules.Factor(ModConfig.TintGreen, strength);
            float b = TintRules.Factor(ModConfig.TintBlue, strength);

            var copies = new Dictionary<int, Material>();
            int tinted = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (As<MeshRenderer>(renderer) == null && As<SkinnedMeshRenderer>(renderer) == null) continue;

                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null || material.renderQueue >= 3000) continue;

                    int key = material.GetInstanceID();
                    if (!copies.TryGetValue(key, out Material copy))
                    {
                        copy = TintedCopy(material, r, g, b);
                        copies[key] = copy;
                        if (copy != null) tinted++;
                    }
                    if (copy == null) continue;
                    materials[j] = copy;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }

            ModLog.Verbose("Tinted " + tinted + " materials of the Brick Mixer.");
            if (tinted == 0) ModLog.Warning("No material of the Mk2 has a color the mod can tint; the Brick Mixer looks like a Mk2.");
        }

        private static Material TintedCopy(Material material, float r, float g, float b)
        {
            string property = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
            if (property == null) return null;

            Material copy = new Material(material);
            copy.name = material.name + " (BrickMixer)";
            copy.hideFlags = HideFlags.HideAndDontSave;
            Color color = material.GetColor(property);
            copy.SetColor(property, new Color(color.r * r, color.g * g, color.b * b, color.a));
            return copy;
        }

        private static BuildableItemDefinition FindMk2(Registry registry)
        {
            var items = registry.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                BuildableItemDefinition definition = As<BuildableItemDefinition>(items[i]);
                if (definition == null || definition.ID == ItemId) continue;
                if (As<MixingStationMk2>(definition.BuiltItem) != null) return definition;
            }
            return null;
        }

        /// <summary>Applies the price setting to the item.</summary>
        public static void RefreshPrice()
        {
            if (_definition != null) _definition.BasePurchasePrice = BrickRules.Price(_mk2Price, ModConfig.PriceMultiplier);
        }

        /// <summary>
        /// Adds the Brick Mixer to a shop that sells the Mk2, before the shop builds its list. A shop that
        /// does not sell the Mk2 is left alone.
        /// </summary>
        public static void AddToShop(ShopInterface shop)
        {
            if (!EnsureRegistered()) return;

            bool sellsMk2 = false;
            var listings = shop.Listings;
            for (int i = 0; i < listings.Count; i++)
            {
                ShopListing listing = listings[i];
                if (listing == null || listing.Item == null) continue;
                if (listing.Item.ID == ItemId) return;
                if (listing.Item.ID == _mk2Id) sellsMk2 = true;
            }
            if (!sellsMk2) return;

            // The shop tints the item's icon, so the listing stands out too.
            var added = new ShopListing { name = ItemName, Item = _definition };
            float strength = ModConfig.TintStrength;
            if (strength > 0f)
            {
                added.UseIconTint = true;
                added.IconTint = new Color(TintRules.Factor(ModConfig.TintRed, strength), TintRules.Factor(ModConfig.TintGreen, strength), TintRules.Factor(ModConfig.TintBlue, strength), 1f);
            }
            listings.Add(added);
            ModLog.Verbose("Listed the Brick Mixer in a shop that sells the Mk2.");
        }

        // ------------------------------------------------------------------------------------
        // The station
        // ------------------------------------------------------------------------------------

        private static bool IsBrickMixer(MixingStation station)
            => station != null && station.ItemInstance != null && station.ItemInstance.ID == ItemId;

        public static bool IsBrickMixer(object stationObject) => IsBrickMixer(As<MixingStation>(stationObject));

        /// <summary>True when the slot is the output slot of a Brick Mixer.</summary>
        public static bool IsBrickOutputSlot(ItemSlot slot)
        {
            if (slot == null) return false;
            MixingStation station = As<MixingStation>(slot.SlotOwner);
            return IsBrickMixer(station) && Same(station.OutputSlot, slot);
        }

        /// <summary>Puts a finished batch back in a brick, when it is a whole number of bricks.</summary>
        public static bool Repack(ref ItemInstance item, out int units, out int bricks)
        {
            units = 0;
            bricks = 0;
            ProductItemInstance product = As<ProductItemInstance>(item);
            if (product == null || product.AppliedPackaging != null) return false;

            units = product.Quantity;
            bricks = BrickRules.RepackBricks(units);
            if (bricks <= 0) return false;

            PackagingDefinition brick = FindBrick();
            if (brick == null) return false;

            ProductItemInstance copy = As<ProductItemInstance>(product.GetCopy(bricks));
            if (copy == null) return false;
            copy.SetPackaging(brick);
            item = copy;
            return true;
        }

        // The brick is the packaging that holds 20 units; found by that, not by a name the game could change.
        private static PackagingDefinition FindBrick()
        {
            if (_brick != null) return _brick;
            Registry registry = Singleton<Registry>.Instance;
            if (registry == null) return null;

            var items = registry.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                PackagingDefinition packaging = As<PackagingDefinition>(items[i]);
                if (packaging != null && BrickRules.IsBrick(packaging.Quantity))
                {
                    _brick = packaging;
                    return _brick;
                }
            }
            ModLog.Warning("No 20-unit packaging was found in the item registry, so a finished mix stays loose.");
            return null;
        }
    }
}
