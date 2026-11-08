using System.Linq;
using System.Reflection;
using Xunit;

namespace MixPackagedDrugs.Tests
{
    /// <summary>
    /// Early warning for game updates. The mod patches game methods by name, which a compiler cannot
    /// check. These tests read the game DLLs in lib/ and fail if a patch target has gone, so after a
    /// game update: refresh lib/, run the tests, and you know before launching the game.
    /// They check that members exist, not that the game still behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void ItemSlot_patch_targets_are_declared_once_with_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type slot = game.GameType("ItemFramework.ItemSlot");

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone, so each must be unique.
            MethodInfo hard = Assert.Single(slot.GetMethods(GameApi.Declared), m => m.Name == "DoesItemMatchHardFilters");
            Assert.Equal(new[] { "ItemInstance" }, hard.GetParameters().Select(p => p.ParameterType.Name));
            Assert.Equal("Boolean", hard.ReturnType.Name);
            Assert.True(hard.IsVirtual);

            MethodInfo capacity = Assert.Single(slot.GetMethods(GameApi.Declared), m => m.Name == "GetCapacityForItem");
            Assert.Equal(new[] { "ItemInstance", "Boolean" }, capacity.GetParameters().Select(p => p.ParameterType.Name));
            Assert.Equal(new[] { "item", "checkPlayerFilters" }, capacity.GetParameters().Select(p => p.Name));   // the patch binds both by name
            Assert.Equal("Int32", capacity.ReturnType.Name);
            Assert.True(capacity.IsVirtual);

            MethodInfo store = Assert.Single(slot.GetMethods(GameApi.Declared), m => m.Name == "SetStoredItem");
            Assert.Equal(new[] { "ItemInstance", "Boolean" }, store.GetParameters().Select(p => p.ParameterType.Name));
            Assert.Equal("instance", store.GetParameters()[0].Name);   // the patch binds `ref instance` by name
            Assert.True(store.IsVirtual);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_slot_and_station_members_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Assert.Equal("IItemSlotOwner", GameApi.ValueType(game.GameType("ItemFramework.ItemSlot"), "SlotOwner", isStatic: false)?.Name);
            Assert.Equal("ItemInstance", GameApi.ValueType(game.GameType("ItemFramework.ItemSlot"), "ItemInstance", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(game.GameType("ItemFramework.ItemSlot"), "Quantity", isStatic: false)?.Name);
            Assert.Equal("ItemSlot", GameApi.ValueType(game.GameType("ObjectScripts.MixingStation"), "ProductSlot", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_packaging_members_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type product = game.GameType("Product.ProductItemInstance");

            Assert.Equal("PackagingDefinition", GameApi.ValueType(product, "AppliedPackaging", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(product, "Amount", isStatic: false)?.Name);

            MethodInfo setPackaging = GameApi.PublicMethod(product, "SetPackaging", "PackagingDefinition");
            Assert.NotNull(setPackaging);

            MethodInfo getCopy = GameApi.PublicMethod(product, "GetCopy", "Int32");
            Assert.NotNull(getCopy);
            Assert.Equal("ItemInstance", getCopy.ReturnType.Name);

            Assert.Equal("Int32", GameApi.ValueType(game.GameType("Product.Packaging.PackagingDefinition"), "Quantity", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_product_slot_still_has_an_unpackaged_filter_class_to_open(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            // The mod does not patch this class; it exists as the reason the slot refuses packages.
            Assert.NotNull(game.GameType("ItemFramework.ItemFilter_UnpackagedProduct"));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Add_and_drag_patch_targets_are_declared_once_with_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            MethodInfo add = Assert.Single(game.GameType("ItemFramework.ItemSlot").GetMethods(GameApi.Declared), m => m.Name == "AddItem");
            Assert.Equal(new[] { "ItemInstance", "Boolean" }, add.GetParameters().Select(p => p.ParameterType.Name));
            Assert.Equal(new[] { "item", "_internal" }, add.GetParameters().Select(p => p.Name));   // the patch binds `ref item` by name

            System.Type ui = game.GameType("UI.Items.ItemUIManager");
            MethodInfo endDrag = Assert.Single(ui.GetMethods(GameApi.Declared), m => m.Name == "EndDrag");
            Assert.Empty(endDrag.GetParameters());
            // Private on Mono, public in the IL2CPP proxy.
            Assert.Equal(game.IsIl2Cpp, endDrag.IsPublic);

            Assert.Equal("ItemSlotUI", GameApi.ValueType(ui, "draggedSlot", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(ui, "draggedAmount", isStatic: false)?.Name);
            Assert.Equal("ItemSlotUI", GameApi.ValueType(ui, "HoveredSlot", isStatic: false)?.Name);
            Assert.Equal("ItemSlot", GameApi.ValueType(game.GameType("UI.ItemSlotUI"), "assignedSlot", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(game.GameType("ItemFramework.ItemInstance"), "StackLimit", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_stack_change_the_drop_uses_exists(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            MethodInfo change = GameApi.PublicMethod(game.GameType("ItemFramework.ItemSlot"), "ChangeQuantity", "Int32", "Boolean");
            Assert.NotNull(change);

            System.Type slot = game.GameType("ItemFramework.ItemSlot");
            Assert.Equal("Boolean", GameApi.ValueType(slot, "IsLocked", isStatic: false)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(slot, "IsAddLocked", isStatic: false)?.Name);

            System.Type ui = game.GameType("UI.Items.ItemUIManager");
            Assert.NotNull(GameApi.PublicMethod(ui, "CanDragFromSlot", "ItemSlotUI"));
            Assert.Equal("UnityEvent", GameApi.ValueType(ui, "onItemMoved", isStatic: false)?.Name);

            // The Brick Mixer's station is recognised by its item's ID.
            Assert.Equal("ItemInstance", GameApi.ValueType(game.GameType("EntityFramework.BuildableItem"), "ItemInstance", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(game.GameType("ItemFramework.ItemInstance"), "ID", isStatic: false)?.Name);
        }
    }
}
