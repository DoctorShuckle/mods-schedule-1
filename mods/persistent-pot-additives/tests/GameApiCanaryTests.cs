using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PersistentPotAdditives.Tests
{
    /// <summary>
    /// Early warning for game updates. The mod finds some game members by name at runtime (Harmony
    /// patch targets, parameter names, a private field), and a compiler cannot check those. These
    /// tests read the game DLLs in lib/ and fail if anything the mod depends on has gone, so after
    /// a game update: refresh lib/, run the tests, and you know before launching the game.
    /// They check that members exist, not that the game still behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Harvest_patch_target_is_one_parameterless_method_on_Pot(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type pot = game.GameType("ObjectScripts.Pot");

            // [HarmonyPatch(typeof(Pot), "OnPlantFullyHarvested")] picks the method by name alone.
            MethodInfo[] matches = pot.GetMethods(GameApi.Declared).Where(m => m.Name == "OnPlantFullyHarvested").ToArray();
            Assert.Single(matches);
            Assert.Empty(matches[0].GetParameters());
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Plant_seed_patch_target_is_one_method_and_keeps_its_parameter_name(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type pot = game.GameType("ObjectScripts.Pot");

            // The hash after the prefix changes with the game build, so the mod matches the prefix.
            // Two matches would make "first one wins" a guess.
            MethodInfo[] matches = pot.GetMethods(GameApi.Declared)
                .Where(m => m.Name.StartsWith("RpcLogic___PlantSeed_Server", StringComparison.Ordinal)).ToArray();
            Assert.Single(matches);

            // Harmony fills the Postfix's `normalizedSeedProgress` argument by matching this name.
            ParameterInfo progress = matches[0].GetParameters().SingleOrDefault(p => p.Name == "normalizedSeedProgress");
            Assert.NotNull(progress);
            Assert.Equal("Single", progress.ParameterType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Pot_members_the_mod_calls_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type pot = game.GameType("ObjectScripts.Pot");

            Assert.NotNull(GameApi.PublicMethod(pot, "ApplyAdditive_Server", "String"));

            MethodInfo isApplied = GameApi.PublicMethod(pot, "IsAdditiveApplied", "String");
            Assert.NotNull(isApplied);
            Assert.Equal("Boolean", isApplied.ReturnType.Name);

            Type plant = GameApi.ValueType(pot, "Plant", isStatic: false);
            Assert.NotNull(plant);
            Assert.Equal("Plant", plant.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Applied_additives_is_a_list_of_AdditiveDefinition(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type pot = game.GameType("ObjectScripts.Pot");

            Type list = GameApi.ValueType(pot, "AppliedAdditives", isStatic: false);
            Assert.NotNull(list);
            Assert.Equal("List`1", list.Name);
            Assert.Equal("AdditiveDefinition", list.GetGenericArguments().Single().Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Additive_definition_has_id_and_instant_growth(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type additive = game.GameType("ItemFramework.AdditiveDefinition");

            Assert.Equal("Single", GameApi.ValueType(additive, "InstantGrowth", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(additive, "ID", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Plant_growth_can_be_read_and_set(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type plant = game.GameType("Growing.Plant");

            Assert.Equal("Single", GameApi.ValueType(plant, "NormalizedGrowthProgress", isStatic: false)?.Name);
            Assert.NotNull(GameApi.PublicMethod(plant, "SetNormalizedGrowthProgress", "Single"));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Remaining_soil_uses_is_reachable_the_way_each_flavor_reads_it(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type container = game.GameType("Growing.GrowContainer");

            if (game.IsIl2Cpp)
            {
                // The proxy exposes it as a public property: `container._remainingSoilUses`.
                PropertyInfo property = container.GetProperty("_remainingSoilUses", BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(property?.GetMethod);
                Assert.Equal("Int32", property.PropertyType.Name);
            }
            else
            {
                // Mono: a protected int field, read through AccessTools.FieldRefAccess by name.
                FieldInfo field = container.GetField("_remainingSoilUses", GameApi.Declared);
                Assert.NotNull(field);
                Assert.Equal("Int32", field.FieldType.Name);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Server_and_load_state_can_be_read(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Assert.Equal("Boolean", GameApi.ValueType(game.FishNetType("InstanceFinder"), "IsServer", isStatic: true)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(game.GameType("Persistence.LoadManager"), "IsLoading", isStatic: false)?.Name);

            // Singleton<LoadManager>.Instance is how the mod reaches the load manager.
            Type singleton = game.GameType("DevUtilities.Singleton`1");
            Assert.NotNull(GameApi.ValueType(singleton, "Instance", isStatic: true));
        }
    }
}
