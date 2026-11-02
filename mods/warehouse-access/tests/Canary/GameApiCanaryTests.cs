using System.Linq;
using System.Reflection;
using Xunit;

namespace WarehouseAccess.Tests
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
        public void Dark_market_patch_targets_are_declared_once_and_answer_a_bool(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            MethodInfo shouldBeOpen = Assert.Single(game.GameType("Map.DarkMarket").GetMethods(GameApi.Declared), m => m.Name == "ShouldBeOpen");
            Assert.Empty(shouldBeOpen.GetParameters());
            Assert.Equal("Boolean", shouldBeOpen.ReturnType.Name);

            MethodInfo getIsOpen = Assert.Single(game.GameType("Map.DarkMarketAccessZone").GetMethods(GameApi.Declared), m => m.Name == "GetIsOpen");
            Assert.Empty(getIsOpen.GetParameters());
            Assert.Equal("Boolean", getIsOpen.ReturnType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Door_and_Igor_patch_targets_are_declared_once_with_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type door = game.GameType("Map.DarkMarketMainDoor");
            Assert.Empty(Assert.Single(door.GetMethods(GameApi.Declared), m => m.Name == "Hovered").GetParameters());
            MethodInfo setKnocking = Assert.Single(door.GetMethods(GameApi.Declared), m => m.Name == "SetKnockingEnabled");
            Assert.Equal("Boolean", Assert.Single(setKnocking.GetParameters()).ParameterType.Name);

            System.Type igor = game.GameType("NPCs.CharacterClasses.Igor");
            Assert.Empty(Assert.Single(igor.GetMethods(GameApi.Declared), m => m.Name == "Start").GetParameters());
            // Public in the IL2CPP proxy, private on Mono: the mod reaches it each way.
            MethodInfo guardOffice = Assert.Single(igor.GetMethods(GameApi.Declared), m => m.Name == "GuardOffice");
            Assert.Empty(guardOffice.GetParameters());
            Assert.Equal(game.IsIl2Cpp, guardOffice.IsPublic);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_quest_and_rank_members_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type quest = game.GameType("Quests.Quest_Warehouse");
            Assert.Equal("EQuestState", GameApi.ValueType(quest, "State", isStatic: false)?.Name);
            Assert.True(game.GameType("Quests.EQuestState").GetField("Completed") != null);

            Assert.Equal("FullRank", GameApi.ValueType(game.GameType("Map.DarkMarket"), "UnlockRank", isStatic: false)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(game.GameType("Map.DarkMarket"), "IsOpen", isStatic: false)?.Name);

            System.Type fullRank = game.GameType("Levelling.FullRank");
            Assert.Contains(fullRank.GetConstructors(), c => c.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(new[] { "ERank", "Int32" }));
            Assert.Contains(fullRank.GetMethods(BindingFlags.Public | BindingFlags.Static), m => m.Name == "op_GreaterThanOrEqual");
            Assert.Contains(game.GameType("Levelling.LevelManager").GetMethods(GameApi.Declared),
                m => m.Name == "GetFullRank" && m.GetParameters().Length == 0 && m.ReturnType.Name == "FullRank");
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_rank_names_the_settings_accept_still_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            string[] names = game.GameType("Levelling.ERank").GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.Name).ToArray();
            Assert.Contains("Street_Rat", names);
            Assert.Contains("Hoodlum", names);
        }
    }
}
