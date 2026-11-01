using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class TaskSpeedFactorTests
    {
        [Theory]
        [InlineData(10f, 5f, 2f)]
        [InlineData(15f, 30f, 0.5f)]
        [InlineData(10f, -1f, 1f)]
        [InlineData(10f, 0f, 1f)]
        [InlineData(0f, 5f, 1f)]
        public void Factor_IsGameSecondsOverWanted(float game, float wanted, float expected)
            => Assert.Equal(expected, TaskTiming.TaskSpeedFactor(game, wanted), 4);

        [Fact]
        public void Factor_IsLimitedLikeScaleDuration()
            => Assert.Equal(10f / TaskTiming.MinSeconds, TaskTiming.TaskSpeedFactor(10f, 0.0001f), 3);
    }

    public class ScaleDurationTests
    {
        [Theory]
        [InlineData(10f, 10f, 5f, 5f)]       // pour soil: 10 s -> 5 s
        [InlineData(7f, 1f, 2f, 14f)]        // harvest 7 plants at 2 s each, not "2 s total"
        [InlineData(10f, 10f, -1f, 10f)]     // unset
        [InlineData(10f, 10f, 0f, 10f)]
        [InlineData(10f, 0f, 5f, 10f)]       // no base to scale from
        [InlineData(10f, 10f, float.NaN, 10f)]
        public void Scales_what_the_game_returned(float gameResult, float gameBase, float configured, float expected)
            => Assert.Equal(expected, TaskTiming.ScaleDuration(gameResult, gameBase, configured));

        [Fact]
        public void A_tiny_duration_is_raised_to_the_minimum()
            => Assert.Equal(10f * TaskTiming.MinSeconds / 10f, TaskTiming.ScaleDuration(10f, 10f, 0.0001f), 5);

        [Fact]
        public void A_huge_duration_is_capped()
            => Assert.Equal(TaskTiming.MaxSeconds, TaskTiming.ScaleDuration(10f, 10f, 1e9f));

        [Fact]
        public void Keeps_working_if_the_game_changes_its_own_number()
            => Assert.Equal(10f, TaskTiming.ScaleDuration(20f, 10f, 5f));   // the game now says 20 s for a 10 s-based task; 5 s means "half"
    }
}
