using System.Linq;
using System.Reflection;
using Xunit;

namespace ConfigurableStorage.Tests
{
    /// <summary>
    /// Early warning for game updates. The mod patches game methods by name and sets fields by name, which a
    /// compiler cannot check. These tests read the game DLLs in lib/ and fail if a target has gone.
    /// They check that members exist, not that the game still behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Load_patch_targets_are_declared_once(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type load = game.GameType("Persistence.LoadManager");

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            Assert.Single(load.GetMethods(GameApi.Declared), m => m.Name == "StartGame");
            MethodInfo asClient = Assert.Single(load.GetMethods(GameApi.Declared), m => m.Name == "LoadAsClient");
            Assert.Equal("String", Assert.Single(asClient.GetParameters()).ParameterType.Name);
            Assert.Empty(Assert.Single(load.GetMethods(GameApi.Declared), m => m.Name == "LoadTutorialAsClient").GetParameters());
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_registry_lists_item_definitions_with_an_id_and_a_name(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            MethodInfo getAll = Assert.Single(game.GameType("Registry").GetMethods(GameApi.Declared), m => m.Name == "GetAllItems");
            Assert.Empty(getAll.GetParameters());

            System.Type definition = game.GameType("ItemFramework.ItemDefinition");
            Assert.Equal("String", GameApi.ValueType(definition, "ID", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(definition, "Name", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void A_buildable_item_points_at_the_prefab_that_is_built(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Assert.Equal("BuildableItem", GameApi.ValueType(game.GameType("ItemFramework.BuildableItemDefinition"), "BuiltItem", isStatic: false)?.Name);
            System.Type placeable = game.GameType("ObjectScripts.PlaceableStorageEntity");
            Assert.True(game.GameType("EntityFramework.BuildableItem").IsAssignableFrom(placeable));
            Assert.Equal("StorageEntity", GameApi.ValueType(placeable, "StorageEntity", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_storage_size_members_the_mod_sets_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // A public field on Mono, a public property on the IL2CPP proxy: the mod writes either the same way.
            System.Type storage = game.GameType("Storage.StorageEntity");
            Assert.Equal("Int32", GameApi.ValueType(storage, "SlotCount", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(storage, "DisplayRowCount", isStatic: false)?.Name);
            Assert.Contains(storage.GetMethods(GameApi.Declared), m => m.Name == "Awake");
        }
    }
}
