using System.Linq;
using System.Reflection;
using Xunit;

namespace ConfigurableSkateboard.Tests
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
        public void Board_patch_targets_are_declared_once_with_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type board = game.GameType("Skating.Skateboard");

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            MethodInfo start = Assert.Single(board.GetMethods(GameApi.Declared), m => m.Name == "Start");
            Assert.Empty(start.GetParameters());

            MethodInfo weather = Assert.Single(board.GetMethods(GameApi.Declared), m => m.Name == "OnWeatherChange");
            Assert.Equal("WeatherConditions", Assert.Single(weather.GetParameters()).ParameterType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void ChangeStamina_is_declared_once_and_takes_the_change_first(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            MethodInfo change = Assert.Single(game.GameType("PlayerScripts.PlayerMovement").GetMethods(GameApi.Declared), m => m.Name == "ChangeStamina");
            ParameterInfo first = change.GetParameters().First();
            Assert.Equal("change", first.Name);
            Assert.Equal("Single", first.ParameterType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_settings_the_mod_reads_and_writes_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type settings = game.GameType("Experimental.SkateboardSettings");
            foreach (string name in new[] { "TopSpeed_Kmh", "PushForceMultiplier", "JumpForce" })
                Assert.Equal("Single", GameApi.ValueType(settings, name, isStatic: false)?.Name);
            Assert.Equal("Single", GameApi.ValueType(settings, "LateralFrictionForceMultiplier", isStatic: false)?.Name);
            Assert.Equal("AnimationCurve", GameApi.ValueType(settings, "LongitudinalFrictionCurve", isStatic: false)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(game.GameType("Skating.Skateboard"), "SlowOnTerrain", isStatic: false)?.Name);
            Assert.NotNull(settings.GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance));

            System.Type board = game.GameType("Skating.Skateboard");
            Assert.Equal("SkateboardSettings", GameApi.ValueType(board, "_settings", isStatic: false)?.Name);
            Assert.Equal("SkateboardSettings", GameApi.ValueType(board, "DefaultSettings", isStatic: false)?.Name);

            Assert.Equal("Skateboard", GameApi.ValueType(game.GameType("PlayerScripts.Player"), "ActiveSkateboard", isStatic: false)?.Name);
            Assert.NotNull(GameApi.ValueType(game.GameType("PlayerScripts.Player"), "Local", isStatic: true));
        }
    }
}
