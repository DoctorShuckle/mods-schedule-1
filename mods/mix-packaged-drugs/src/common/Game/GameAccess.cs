using HarmonyLib;

namespace MixPackagedDrugs
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
#if !IL2CPP
        // Private on Mono; the IL2CPP proxy exposes them as public.
        private static readonly AccessTools.FieldRef<ItemUIManager, ItemSlotUI> DraggedSlotRef =
            AccessTools.FieldRefAccess<ItemUIManager, ItemSlotUI>("draggedSlot");
        private static readonly AccessTools.FieldRef<ItemUIManager, int> DraggedAmountRef =
            AccessTools.FieldRefAccess<ItemUIManager, int>("draggedAmount");
        private static readonly System.Reflection.MethodInfo SetHoveredSlot = AccessTools.PropertySetter(typeof(ItemUIManager), "HoveredSlot");
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

        /// <summary>Whether the mod is switched on.</summary>
        public static bool IsEnabled => ModConfig.Enabled;

        /// <summary>
        /// The same product unpackaged: quantity times the package's units. The copy keeps the product's own
        /// subclass and quality. Null when the item is not a packaged product.
        /// </summary>
        public static ItemInstance Unpackaged(ItemInstance item)
        {
            ProductItemInstance product = AsProduct(item);
            if (product == null || product.AppliedPackaging == null) return null;

            ProductItemInstance copy = AsProduct(product.GetCopy(Packages.Units(product.Quantity, product.Amount)));
            if (copy == null) return null;
            copy.SetPackaging(null);
            return copy;
        }

        /// <summary>
        /// Whole packages of this item the slot has room for. The room is the game's own answer for the item
        /// unpackaged (so a different product or quality, a lock or a full stack all give 0), divided by the
        /// package's units. The game cannot stack a package on a loose stack, so it would answer 0 itself.
        /// </summary>
        public static int PackageCapacity(ItemSlot slot, ItemInstance item, bool checkPlayerFilters)
        {
            ItemInstance loose = Unpackaged(item);
            if (loose == null) return 0;
            return Packages.PackagesForRoom(slot.GetCapacityForItem(loose, checkPlayerFilters), AsProduct(item).Amount);
        }

        /// <summary>
        /// Called as a drag ends. A stack of packaged product dropped on a mixing station's product slot is
        /// cut down to the whole packages that fit, or the drop is cancelled when none do. The game itself
        /// does not check room when the slot is empty, and would swap the two stacks when the slot holds loose
        /// product. In that case the packages are unpacked into the loose stack here instead, and the game's
        /// own drop is cancelled.
        /// </summary>
        public static void ClampMixerDrop(ItemUIManager ui)
        {
            if (!IsEnabled) return;
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
            if (!IsPackagedProduct(source.ItemInstance, out int amount)) return;
            // The game's own drop does nothing in these cases; the merge below must not either.
            if (!ui.CanDragFromSlot(dragged) || target.IsLocked || target.IsAddLocked) return;

#if IL2CPP
            int dragging = ui.draggedAmount;
#else
            int dragging = DraggedAmountRef(ui);
#endif
            int fit = PackageCapacity(target, source.ItemInstance, false);
            switch (Packages.PlanDrop(target.ItemInstance != null, fit, dragging, out int count))
            {
                case Packages.DropAction.Merge:
                    // Add to the loose stack ourselves, as the game does for two stacks that match.
                    target.ChangeQuantity(Packages.Units(count, amount));
                    source.ChangeQuantity(-count);
                    ModLog.Verbose("Unpacked " + count + " package(s) of " + amount + " into the mixing station's stack.");
                    CancelDrop(ui);
                    ui.onItemMoved?.Invoke();
                    break;
                case Packages.DropAction.Cancel:
                    CancelDrop(ui);
                    ModLog.Verbose("Cancelled a drop of packaged product: no room, or a different product, in the mixing station's product slot.");
                    break;
                case Packages.DropAction.Cut:
#if IL2CPP
                    ui.draggedAmount = count;
#else
                    DraggedAmountRef(ui) = count;
#endif
                    ModLog.Verbose("Cut a drop of packaged product from " + dragging + " to " + count + " to fit the slot.");
                    break;
            }
        }

        // The game does nothing for a drag that ends over no slot.
        private static void CancelDrop(ItemUIManager ui)
        {
#if IL2CPP
            ui.HoveredSlot = null;
#else
            SetHoveredSlot.Invoke(ui, new object[] { null });
#endif
        }
    }
}
