using System.Linq;
using System.Reflection;
using Xunit;

namespace ConfigurableAtm.Tests
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

        private static MethodInfo Declared(System.Type type, string name)
            => Assert.Single(type.GetMethods(GameApi.Declared), m => m.Name == name);

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Patch_targets_are_declared_once_with_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type atm = game.GameType("UI.ATMInterface");

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            Assert.Empty(Declared(atm, "Start").GetParameters());
            Assert.Empty(Declared(atm, "Update").GetParameters());
            Assert.Empty(Declared(atm, "UpdateAvailableAmounts").GetParameters());
            Assert.Empty(Declared(game.GameType("Quests.Quest_CleanCash"), "OnUncappedMinPass").GetParameters());
            Assert.Equal("Single", Declared(atm, "get_remainingAllowedDeposit").ReturnType.Name);
        }

        [SkippableFact]
        public void Il2Cpp_shifted_methods_and_quest_calls_exist()
        {
            using GameApi game = GameApi.Open("il2cpp");
            System.Type atm = game.GameType("UI.ATMInterface");

            // Each is wrapped by name, so each must be declared exactly once.
            foreach (string name in new[] { "SetActiveScreen", "AmountSelected", "DefaultAmountSelection", "ChangeAmount", "GetAmountFromIndex", "SetSelectedAmount" })
                Declared(atm, name);

            // The postfix on the quest calls Begin(bool) and reads State.
            Assert.NotNull(GameApi.PublicMethod(game.GameType("Quests.Quest"), "Begin", "Boolean"));
            Assert.Equal("EQuestState", GameApi.ValueType(game.GameType("Quests.Quest"), "State", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_members_the_mod_reads_and_writes_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type atm = game.GameType("UI.ATMInterface");

            // An int[] on Mono; the proxy's own array type on IL2CPP.
            Assert.Contains(GameApi.ValueType(atm, "amounts", isStatic: true)?.Name, new[] { "Int32[]", "Il2CppStructArray`1" });
            Assert.Equal("RectTransform", GameApi.ValueType(atm, "depositLimitContainer", isStatic: false)?.Name);
            Assert.Equal("Single", GameApi.ValueType(game.GameType("Money.ATM"), "WeeklyDepositSum", isStatic: true)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(atm, "depositing", isStatic: false)?.Name);

            // Only the IL2CPP patches use these, but a Mono game that lost them has changed too.
            foreach (string name in new[] { "depositLimitText", "amountLabelText" })
                Assert.Equal("Text", GameApi.ValueType(atm, name, isStatic: false)?.Name);
            foreach (string name in new[] { "menuScreen", "amountSelectorScreen", "activeScreen" })
                Assert.Equal("RectTransform", GameApi.ValueType(atm, name, isStatic: false)?.Name);
            Assert.Equal("Button", GameApi.ValueType(atm, "menu_DepositButton", isStatic: false)?.Name);
        }

        /// <summary>
        /// The Mono transpilers replace the literal 10000 in these methods and expect an exact count.
        /// Read the IL (nothing is run) and count <c>ldc.r4 10000</c>: if a game update moves the
        /// literal, the count changes here before the patch fails in the game.
        /// </summary>
        [SkippableTheory]
        [InlineData("UI.ATMInterface", "get_remainingAllowedDeposit", 1)]
        [InlineData("UI.ATMInterface", "Update", 3)]
        [InlineData("UI.ATMInterface", "UpdateAvailableAmounts", 1)]
        [InlineData("Quests.Quest_CleanCash", "OnUncappedMinPass", 1)]
        public void Mono_methods_still_hold_the_limit_literal_the_transpilers_replace(string type, string method, int expected)
        {
            using GameApi game = GameApi.Open("mono");
            byte[] il = Declared(game.GameType(type), method).GetMethodBody().GetILAsByteArray();

            // ldc.r4 (0x22) followed by 10000f as little-endian IEEE-754 (0x461C4000).
            byte[] literal = { 0x22, 0x00, 0x40, 0x1C, 0x46 };
            int count = 0;
            for (int i = 0; i + literal.Length <= il.Length; i++)
                if (il.Skip(i).Take(literal.Length).SequenceEqual(literal)) count++;

            Assert.Equal(expected, count);
        }
    }
}
