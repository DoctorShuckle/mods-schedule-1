using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TidyStations.Tests
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
        public void Each_behaviour_declares_GetTrashPrefab_once_returning_a_TrashItem(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            foreach (string name in new[] { "AddSoilToGrowContainerBehaviour", "ApplyAdditiveToGrowContainerBehaviour", "SowSeedInPotBehaviour" })
            {
                // [HarmonyPatch(typeof(X), "GetTrashPrefab")] picks the method by name alone.
                MethodInfo method = Assert.Single(game.GameType("NPCs.Behaviour." + name).GetMethods(GameApi.Declared), m => m.Name == "GetTrashPrefab");
                Assert.Equal("TrashItem", method.ReturnType.Name);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_base_behaviour_still_asks_GetTrashPrefab(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // The overrides only matter while the base class is what decides to drop the item.
            MethodInfo method = Assert.Single(game.GameType("NPCs.Behaviour.GrowContainerBehaviour").GetMethods(GameApi.Declared), m => m.Name == "GetTrashPrefab");
            Assert.True(method.IsVirtual);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Station_trash_methods_are_each_declared_once_and_the_plant_scrap_hook_exists(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // [HarmonyPatch(typeof(X), "CreateTrash") picks the method by name alone, so a second
            // overload would make the patch ambiguous.
            Assert.Single(game.GameType("ObjectScripts.ChemistryStation").GetMethods(GameApi.Declared), m => m.Name == "CreateTrash");
            Assert.Single(game.GameType("ObjectScripts.Cauldron").GetMethods(GameApi.Declared), m => m.Name == "CreateTrash");

            MethodInfo mixing = Assert.Single(game.GameType("PlayerTasks.Tasks.UseMixingStationTask").GetMethods(GameApi.Declared), m => m.Name == "CreateTrash");
            Assert.Empty(mixing.GetParameters());

            Type plant = game.GameType("Growing.Plant");
            MethodInfo harvested = Assert.Single(plant.GetMethods(GameApi.Declared), m => m.Name == "OnFullyHarvested");
            Assert.Empty(harvested.GetParameters());
            Assert.Equal("TrashItem", GameApi.ValueType(plant, "PlantScrapPrefab", isStatic: false)?.Name);
        }
    }
}
