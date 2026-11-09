using System.Linq;
using System.Reflection;
using Xunit;

namespace BrickMixer.Tests
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
        public void AddItem_patch_target_is_declared_once_with_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type slot = game.GameType("ItemFramework.ItemSlot");

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone, so each must be unique.
            MethodInfo add = Assert.Single(slot.GetMethods(GameApi.Declared), m => m.Name == "AddItem");
            Assert.Equal(new[] { "item", "_internal" }, add.GetParameters().Select(p => p.Name));   // the patch binds `ref item` by name
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Station_patch_targets_and_members_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type station = game.GameType("ObjectScripts.MixingStation");

            MethodInfo mix = Assert.Single(station.GetMethods(GameApi.Declared), m => m.Name == "GetMixQuantity");
            Assert.Empty(mix.GetParameters());
            Assert.Equal("Int32", mix.ReturnType.Name);

            MethodInfo start = Assert.Single(station.GetMethods(GameApi.Declared), m => m.Name == "CanStartMix");
            Assert.Empty(start.GetParameters());
            Assert.Equal("Boolean", start.ReturnType.Name);

            Assert.Equal("ItemSlot", GameApi.ValueType(station, "ProductSlot", isStatic: false)?.Name);
            Assert.Equal("ItemSlot", GameApi.ValueType(station, "OutputSlot", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(station, "MaxMixQuantity", isStatic: false)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(station, "RequiresIngredientInsertion", isStatic: false)?.Name);
            Assert.Equal("ItemInstance", GameApi.ValueType(game.GameType("EntityFramework.BuildableItem"), "ItemInstance", isStatic: false)?.Name);
            Assert.Equal("IItemSlotOwner", GameApi.ValueType(game.GameType("ItemFramework.ItemSlot"), "SlotOwner", isStatic: false)?.Name);
            Assert.True(game.GameType("ObjectScripts.MixingStationMk2").IsSubclassOf(station));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Item_registry_and_shop_members_the_mod_uses_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type registry = game.GameType("Registry");
            Assert.Contains(registry.GetMethods(GameApi.Declared), m => m.Name == "AddToRegistry" && !m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "ItemDefinition");
            Assert.Contains(registry.GetMethods(GameApi.Declared), m => m.Name == "GetAllItems" && m.GetParameters().Length == 0);
            Assert.Contains(registry.GetMethods(GameApi.Declared), m => m.Name == "ItemExists" && m.IsStatic && m.GetParameters().Length == 1 && m.ReturnType.Name == "Boolean");

            // The mod writes these on a copy of the Mk2's item.
            System.Type definition = game.GameType("ItemFramework.BuildableItemDefinition");
            Assert.Equal("BuildableItem", GameApi.ValueType(definition, "BuiltItem", isStatic: false)?.Name);
            Assert.Equal("Single", GameApi.ValueType(definition, "BasePurchasePrice", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(definition, "ID", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(definition, "Name", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(definition, "Description", isStatic: false)?.Name);

            System.Type shop = game.GameType("UI.Shop.ShopInterface");
            MethodInfo awake = Assert.Single(shop.GetMethods(GameApi.Declared), m => m.Name == "Awake");
            Assert.Empty(awake.GetParameters());
            Assert.Equal("List`1", GameApi.ValueType(shop, "Listings", isStatic: false)?.Name);

            System.Type listing = game.GameType("UI.Shop.ShopListing");
            Assert.Equal("String", GameApi.ValueType(listing, "name", isStatic: false)?.Name);
            Assert.Equal("StorableItemDefinition", GameApi.ValueType(listing, "Item", isStatic: false)?.Name);
            Assert.NotNull(listing.GetConstructor(System.Type.EmptyTypes));
            Assert.Equal("Boolean", GameApi.ValueType(listing, "UseIconTint", isStatic: false)?.Name);
            Assert.Equal("Color", GameApi.ValueType(listing, "IconTint", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void StartGame_is_declared_once_and_the_packaging_members_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // Only the name is patched, so a second overload would be patched too.
            MethodInfo start = Assert.Single(game.GameType("Persistence.LoadManager").GetMethods(GameApi.Declared), m => m.Name == "StartGame");
            Assert.Equal("SaveInfo", start.GetParameters()[0].ParameterType.Name);

            System.Type product = game.GameType("Product.ProductItemInstance");
            Assert.Equal("PackagingDefinition", GameApi.ValueType(product, "AppliedPackaging", isStatic: false)?.Name);
            Assert.NotNull(GameApi.PublicMethod(product, "SetPackaging", "PackagingDefinition"));
            Assert.Equal("ItemInstance", GameApi.PublicMethod(product, "GetCopy", "Int32")?.ReturnType.Name);
            Assert.Equal("Int32", GameApi.ValueType(game.GameType("Product.Packaging.PackagingDefinition"), "Quantity", isStatic: false)?.Name);
        }
    }
}
