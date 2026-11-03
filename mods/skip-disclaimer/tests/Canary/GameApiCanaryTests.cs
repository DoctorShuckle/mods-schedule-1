using System.Linq;
using System.Reflection;
using Xunit;

namespace SkipDisclaimer.Tests
{
    /// <summary>
    /// Early warning for game updates: reads the game DLLs in lib/ and fails if a patch target has
    /// gone. Checks that members exist, not that the game still behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Disclaimer_Awake_is_declared_once_and_takes_no_arguments(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            System.Type disclaimer = game.GameType("UI.MainMenu.Disclaimer");
            MethodInfo awake = Assert.Single(disclaimer.GetMethods(GameApi.Declared), m => m.Name == "Awake");
            Assert.Empty(awake.GetParameters());
            Assert.Equal("Void", awake.ReturnType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Disclaimer_Shown_is_a_public_static_bool_the_mod_can_set(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type disclaimer = game.GameType("UI.MainMenu.Disclaimer");
            // A public field on Mono, a public static property on the IL2CPP proxy.
            Assert.Equal("Boolean", GameApi.ValueType(disclaimer, "Shown", isStatic: true)?.Name);
            if (game.IsIl2Cpp)
            {
                PropertyInfo shown = disclaimer.GetProperty("Shown", BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(shown?.SetMethod);
            }
            else
            {
                Assert.True(disclaimer.GetField("Shown", BindingFlags.Public | BindingFlags.Static) != null);
            }
        }
    }
}
