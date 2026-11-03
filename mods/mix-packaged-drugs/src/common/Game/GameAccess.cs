using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MelonLoader.Utils;

namespace MixPackagedDrugs
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
        // What each mixing station last received packaged, by the station's GUID: the output of its mixes is put
        // back in this packaging. Kept on the host only. It is written to a small file of the mod's own whenever
        // the game saves (the game deletes unknown files from its save folder, so the file lives in UserData) and
        // read back the first time it is needed after a save is loaded.
        private static Dictionary<string, string> _packagingByStation = new Dictionary<string, string>();
        private static string _loadedForSave;

#if !IL2CPP
        // Private on Mono; the IL2CPP proxy exposes them as public.
        private static readonly AccessTools.FieldRef<ItemUIManager, ItemSlotUI> DraggedSlotRef =
            AccessTools.FieldRefAccess<ItemUIManager, ItemSlotUI>("draggedSlot");
        private static readonly AccessTools.FieldRef<ItemUIManager, int> DraggedAmountRef =
            AccessTools.FieldRefAccess<ItemUIManager, int>("draggedAmount");
        private static readonly System.Reflection.MethodInfo SetHoveredSlot = AccessTools.PropertySetter(typeof(ItemUIManager), "HoveredSlot");
#endif

        private static string GuidOf(MixingStation station) => station.GUID.ToString();

        private static string SavePath()
        {
            LoadManager loads = Singleton<LoadManager>.Instance;
            return loads == null ? string.Empty : loads.LoadedGameFolderPath ?? string.Empty;
        }

        private static string MemoryFile(string savePath)
            => Path.Combine(MelonEnvironment.UserDataDirectory, "MixPackagedDrugs", PackagingMemory.FileNameFor(savePath));

        // Starts from the file of the save that is loaded now; a different save starts from its own file.
        private static void EnsureLoaded()
        {
            string savePath = SavePath();
            if (savePath == _loadedForSave) return;

            _loadedForSave = savePath;
            _packagingByStation = new Dictionary<string, string>();
            if (savePath.Length == 0) return;
            try
            {
                string file = MemoryFile(savePath);
                if (File.Exists(file)) _packagingByStation = PackagingMemory.Parse(File.ReadAllText(file));
                ModLog.Verbose("Read " + _packagingByStation.Count + " remembered packagings from " + file);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not read the remembered packagings; mixes in progress will come out loose: " + ex.Message);
            }
        }

        /// <summary>Writes the remembered packagings for the loaded save. Called when the game saves.</summary>
        public static void WritePackagingMemory()
        {
            EnsureLoaded();
            string savePath = _loadedForSave;
            if (string.IsNullOrEmpty(savePath)) return;

            string file = MemoryFile(savePath);
            if (_packagingByStation.Count == 0)
            {
                if (File.Exists(file)) File.Delete(file);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, PackagingMemory.Serialize(_packagingByStation));
            ModLog.Verbose("Wrote " + _packagingByStation.Count + " remembered packagings to " + file);
        }

        /// <summary>Forgets a station's packaging; called when the station is removed.</summary>
        public static void ForgetPackaging(MixingStation station)
        {
            EnsureLoaded();
            _packagingByStation.Remove(GuidOf(station));
        }

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

        /// <summary>True when the slot is the product slot of a mixing station (not the mixer or output slot).</summary>
        public static bool IsMixerProductSlot(ItemSlot slot)
        {
            if (slot == null) return false;
            MixingStation station = As<MixingStation>(slot.SlotOwner);
            return station != null && Same(station.ProductSlot, slot);
        }

        /// <summary>The item as a product, or null when it is anything else.</summary>
        public static ProductItemInstance AsProduct(ItemInstance item) => As<ProductItemInstance>(item);

        /// <summary>True when the item is a product with packaging; the units it holds per package are in <paramref name="amount"/>.</summary>
        public static bool IsPackagedProduct(ItemInstance item, out int amount)
        {
            ProductItemInstance product = AsProduct(item);
            if (product == null || product.AppliedPackaging == null)
            {
                amount = 1;
                return false;
            }
            amount = product.Amount;
            return true;
        }

        /// <summary>Whether the settings let a package of this many units into the slot.</summary>
        public static bool IsAllowed(int amount)
            => Packages.IsAllowed(amount, ModConfig.AllowBags, ModConfig.AllowJars, ModConfig.AllowBricks, allowOther: true);

        /// <summary>True on the host (or in single player), the only machine that unpacks.</summary>
        public static bool IsServer => InstanceFinder.IsServer;

        /// <summary>True when the slot is the output slot of a mixing station.</summary>
        public static bool IsMixerOutputSlot(ItemSlot slot)
        {
            if (slot == null) return false;
            MixingStation station = As<MixingStation>(slot.SlotOwner);
            return station != null && Same(station.OutputSlot, slot);
        }

        /// <summary>
        /// Replaces a packaged stack in the slot with the same product unpackaged: quantity times the
        /// package's units. The copy keeps the product's own subclass and quality. The slot's setter
        /// replicates the change to the other players. The packaging is remembered for the station's output.
        /// </summary>
        public static bool Unpack(ItemSlot slot)
        {
            ProductItemInstance product = AsProduct(slot.ItemInstance);
            if (product == null || product.AppliedPackaging == null) return false;

            PackagingDefinition packaging = product.AppliedPackaging;
            int units = Packages.Units(product.Quantity, product.Amount);
            ProductItemInstance copy = AsProduct(product.GetCopy(units));
            if (copy == null) return false;
            copy.SetPackaging(null);
            slot.SetStoredItem(copy);

            MixingStation station = As<MixingStation>(slot.SlotOwner);
            if (station != null)
            {
                EnsureLoaded();
                _packagingByStation[GuidOf(station)] = packaging.ID;
            }
            return true;
        }

        /// <summary>Forgets the packaging of a station whose product slot was filled with loose product.</summary>
        public static void ForgetPackaging(ItemSlot slot)
        {
            MixingStation station = As<MixingStation>(slot.SlotOwner);
            if (station != null) ForgetPackaging(station);
        }

        /// <summary>True when the slot holds a loose product (not packaged, not empty).</summary>
        public static bool IsLooseProduct(ItemInstance item)
        {
            ProductItemInstance product = AsProduct(item);
            return product != null && product.AppliedPackaging == null;
        }

        /// <summary>
        /// Puts a finished mix back in the packaging the station last received, when the batch fills whole
        /// packages. Returns false (item unchanged) when there is nothing to do.
        /// </summary>
        public static bool Repack(ItemSlot outputSlot, ref ItemInstance item, out int units, out int packages)
        {
            units = 0;
            packages = 0;
            ProductItemInstance product = AsProduct(item);
            if (product == null || product.AppliedPackaging != null) return false;

            MixingStation station = As<MixingStation>(outputSlot.SlotOwner);
            if (station == null) return false;
            EnsureLoaded();
            if (!_packagingByStation.TryGetValue(GuidOf(station), out string packagingId)) return false;
            PackagingDefinition packaging = As<PackagingDefinition>(Registry.GetItem(packagingId));
            if (packaging == null)
            {
                ModLog.Warning("The remembered packaging \"" + packagingId + "\" is not in the game; the mix stays loose.");
                return false;
            }

            units = product.Quantity;
            packages = Packages.RepackQuantity(units, packaging.Quantity);
            if (packages <= 0) return false;

            ProductItemInstance copy = AsProduct(product.GetCopy(packages));
            if (copy == null) return false;
            copy.SetPackaging(packaging);
            item = copy;
            return true;
        }

        /// <summary>
        /// Called as a drag ends. A stack of packaged product dropped on a mixing station's product slot is
        /// cut down to the whole packages that fit, or the drop is cancelled when none do or the slot holds
        /// loose product (the game would swap the two stacks). The game itself does not check room when
        /// the slot is empty.
        /// </summary>
        public static void ClampMixerDrop(ItemUIManager ui)
        {
#if IL2CPP
            ItemSlotUI dragged = ui.draggedSlot;
#else
            ItemSlotUI dragged = DraggedSlotRef(ui);
#endif
            ItemSlotUI hovered = ui.HoveredSlot;
            if (dragged == null || hovered == null || hovered == dragged) return;

            ItemSlot target = hovered.assignedSlot;
            ItemSlot source = dragged.assignedSlot;
            if (target == null || source == null || !IsMixerProductSlot(target)) return;
            if (!IsPackagedProduct(source.ItemInstance, out int amount) || !IsAllowed(amount)) return;

            int fit = target.ItemInstance != null
                ? 0
                : Packages.PackagesThatFit(source.ItemInstance.StackLimit, 0, amount);
#if IL2CPP
            int dragging = ui.draggedAmount;
#else
            int dragging = DraggedAmountRef(ui);
#endif
            if (fit <= 0)
            {
#if IL2CPP
                ui.HoveredSlot = null;
#else
                SetHoveredSlot.Invoke(ui, new object[] { null });
#endif
                ModLog.Verbose("Cancelled a drop of packaged product: no room in the mixing station's product slot.");
            }
            else if (dragging > fit)
            {
#if IL2CPP
                ui.draggedAmount = fit;
#else
                DraggedAmountRef(ui) = fit;
#endif
                ModLog.Verbose("Cut a drop of packaged product from " + dragging + " to " + fit + " to fit the slot.");
            }
        }
    }
}
