using Xunit;

namespace ConfigurableAtm.Tests
{
    /// <summary>The checks the game makes, written against a real limit: what the shifted total must reproduce.</summary>
    internal static class Oracle
    {
        public static bool AtLimit(float weeklySum, float limit) => weeklySum >= limit;

        public static bool Fits(float weeklySum, float amount, float limit) => weeklySum + amount <= limit;

        public static float Remaining(float limit, float weeklySum) => System.Math.Max(0f, limit - weeklySum);
    }

    public class LimitTests
    {
        [Theory]
        [InlineData(10000f, 10000f)]
        [InlineData(250000f, 250000f)]
        [InlineData(500f, 500f)]
        [InlineData(1e12f, DepositRules.MaxLimit)]   // capped, not rejected
        [InlineData(0f, DepositRules.GameLimit)]      // a locked ATM is not a setting
        [InlineData(-5f, DepositRules.GameLimit)]
        [InlineData(float.NaN, DepositRules.GameLimit)]
        [InlineData(float.PositiveInfinity, DepositRules.GameLimit)]
        public void Must_be_positive_and_is_capped(float configured, float expected)
            => Assert.Equal(expected, DepositRules.Limit(configured));
    }

    public class EffectiveLimitTests
    {
        [Fact]
        public void Is_the_configured_limit_when_enabled() => Assert.Equal(25000f, DepositRules.EffectiveLimit(true, 25000f));

        [Fact]
        public void Is_the_sanitized_limit_when_enabled() => Assert.Equal(DepositRules.GameLimit, DepositRules.EffectiveLimit(true, -1f));

        [Fact]
        public void Is_the_largest_float_when_disabled() => Assert.Equal(float.MaxValue, DepositRules.EffectiveLimit(false, 25000f));

        [Fact]
        public void Disabled_limit_never_trips_the_games_arithmetic()
        {
            float limit = DepositRules.EffectiveLimit(false, 1f);
            Assert.False(float.IsInfinity(limit - 5000000f));
            Assert.True(Oracle.Fits(5000000f, 5000f, limit));
            Assert.False(Oracle.AtLimit(5000000f, limit));
        }
    }

    public class QuestThresholdTests
    {
        [Theory]
        [InlineData(true, 5000f, 5000f)]        // lower limit: the quest must stay reachable
        [InlineData(true, 10000f, 10000f)]
        [InlineData(true, 50000f, 10000f)]      // higher limit: the quest does not wait
        [InlineData(false, 5000f, 10000f)]      // no limit: the game's own point
        [InlineData(true, -1f, 10000f)]         // unusable setting: the game's own point
        public void Is_the_smaller_of_the_limit_and_the_games(bool enabled, float configured, float expected)
            => Assert.Equal(expected, DepositRules.QuestThreshold(enabled, configured));
    }

    public class PresetTests
    {
        [Theory]
        [InlineData("20, 50, 100, 500, 1000, 5000", new[] { 20, 50, 100, 500, 1000, 5000 })]
        [InlineData("1,2,3,4,5,6", new[] { 1, 2, 3, 4, 5, 6 })]
        [InlineData("100;200;300;400;500;600", new[] { 100, 200, 300, 400, 500, 600 })]
        [InlineData("  100   200 300,400 ,500, 600  ", new[] { 100, 200, 300, 400, 500, 600 })]
        [InlineData("5000, 1000, 500, 100, 50, 20", new[] { 5000, 1000, 500, 100, 50, 20 })]   // order is the player's
        public void Reads_six_whole_numbers(string configured, int[] expected)
        {
            Assert.True(DepositRules.TryParsePresets(configured, out int[] amounts));
            Assert.Equal(expected, amounts);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("20, 50, 100")]                       // too few
        [InlineData("1, 2, 3, 4, 5, 6, 7")]               // too many
        [InlineData("20, 50, 100, 500, 1000, lots")]      // not a number
        [InlineData("20, 50, 100, 500, 1000, 5.5")]       // not whole
        [InlineData("20, 50, 100, 500, 1000, 0")]         // a button for nothing
        [InlineData("20, 50, 100, 500, 1000, -5")]
        [InlineData("20, 50, 100, 500, 1000, 1000000001")] // beyond the cap
        [InlineData("20, 50, 100, 500, 1000, 99999999999")] // beyond int
        public void Rejects_the_whole_row_when_any_part_is_unusable(string configured)
        {
            Assert.False(DepositRules.TryParsePresets(configured, out int[] amounts));
            Assert.Null(amounts);
        }

        [Fact]
        public void Default_matches_the_buttons_on_the_screen() => Assert.Equal(DepositRules.PresetCount, DepositRules.DefaultPresets.Length);

        [Fact]
        public void Default_setting_text_parses_to_the_default()
        {
            Assert.True(DepositRules.TryParsePresets("20, 50, 100, 500, 1000, 5000", out int[] amounts));
            Assert.Equal(DepositRules.DefaultPresets, amounts);
        }
    }
}
