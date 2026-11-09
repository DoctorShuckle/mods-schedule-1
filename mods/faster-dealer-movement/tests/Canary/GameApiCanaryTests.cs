using System.Linq;
using System.Reflection;
using Xunit;

namespace FasterDealerMovement.Tests
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
        public void The_four_behaviour_hooks_are_declared_once_on_the_dealer_behaviour(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type behaviour = game.GameType("NPCs.Behaviour.DealerAttendDealBehaviour");

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone, so each must be declared
            // on this class (not inherited) and exactly once.
            foreach (string name in new[] { "Activate", "Resume", "Pause", "Deactivate" })
            {
                MethodInfo method = Assert.Single(behaviour.GetMethods(GameApi.Declared), m => m.Name == name);
                Assert.Empty(method.GetParameters());
                Assert.Equal("Void", method.ReturnType.Name);
                Assert.True(method.IsVirtual, name + " should be a virtual override.");
            }
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_speed_stack_members_the_mod_calls_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type controller = game.GameType("NPCs.NPCSpeedController");
            MethodInfo add = Assert.Single(controller.GetMethods(GameApi.Declared),
                m => m.IsPublic && m.Name == "AddSpeedControl" && m.GetParameters().Length == 4);
            Assert.Equal(new[] { "String", "Single", "Int32", "EType" }, add.GetParameters().Select(p => p.ParameterType.Name).ToArray());

            MethodInfo remove = Assert.Single(controller.GetMethods(GameApi.Declared), m => m.IsPublic && m.Name == "RemoveSpeedControl");
            Assert.Equal("String", Assert.Single(remove.GetParameters()).ParameterType.Name);

            System.Type kind = game.GameType("NPCs.SpeedControl").GetNestedType("EType");
            Assert.NotNull(kind);
            Assert.True(kind.GetField("Absolute") != null);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_path_from_a_behaviour_to_its_speed_stack_exists(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Assert.Equal("NPC", GameApi.ValueType(game.GameType("NPCs.Behaviour.Behaviour"), "Npc", isStatic: false)?.Name);
            Assert.Equal("NPCMovement", GameApi.ValueType(game.GameType("NPCs.NPC"), "Movement", isStatic: false)?.Name);
            Assert.Equal("NPCSpeedController", GameApi.ValueType(game.GameType("NPCs.NPCMovement"), "SpeedController", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(game.GameType("NPCs.NPC"), "FullName", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Cartel_dealers_are_a_subclass_of_dealers_and_dealers_are_npcs(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type dealer = game.GameType("Economy.Dealer");
            Assert.Equal(dealer.FullName, game.GameType("Cartel.CartelDealer").BaseType?.FullName);
            Assert.Equal(game.GameType("NPCs.NPC").FullName, dealer.BaseType?.FullName);
        }
    }
}
