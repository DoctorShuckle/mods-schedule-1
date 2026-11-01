using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TidyStreets.Tests
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
        public void Spawner_patch_targets_are_declared_once_and_have_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            foreach (string type in new[] { "TrashGenerator", "TrashSpawnVolume" })
            {
                MethodInfo sleep = Assert.Single(game.GameType("Trash." + type).GetMethods(GameApi.Declared), m => m.Name == "SleepStart");
                Assert.Empty(sleep.GetParameters());
            }

            MethodInfo generate = Assert.Single(game.GameType("Trash.TrashGenerationVolume").GetMethods(GameApi.Declared), m => m.Name == "Generate");
            Assert.Equal("Int32", Assert.Single(generate.GetParameters()).ParameterType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_clear_and_load_hooks_the_mod_calls_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Type manager = game.GameType("Trash.TrashManager");
            MethodInfo clear = GameApi.PublicMethod(manager, "DestroyAllTrash");
            Assert.NotNull(clear);

            Type load = game.GameType("Persistence.LoadManager");
            Assert.Equal("UnityEvent", GameApi.ValueType(load, "onLoadComplete", isStatic: false)?.Name);
        }
    }
}
