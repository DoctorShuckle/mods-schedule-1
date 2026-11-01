using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class SettingTests
    {
        [Theory]
        [InlineData(250f, 100f, 250f)]
        [InlineData(0f, 100f, 0f)]          // free is a valid price
        [InlineData(-1f, 100f, 100f)]       // unset: keep the game's
        [InlineData(float.NaN, 100f, 100f)]
        [InlineData(float.PositiveInfinity, 100f, 100f)]
        public void Money_uses_the_setting_when_it_is_a_price(float configured, float game, float expected)
            => Assert.Equal(expected, Setting.ResolveMoney(configured, game));

        [Theory]
        [InlineData(3f, 1.8f, 3f)]
        [InlineData(500f, 1.8f, Setting.MaxWalkSpeed)]   // capped, not rejected
        [InlineData(0f, 1.8f, 1.8f)]                        // standing still is not a speed
        [InlineData(-1f, 1.8f, 1.8f)]
        [InlineData(float.NaN, 1.8f, 1.8f)]
        public void Speed_must_be_positive_and_is_capped(float configured, float game, float expected)
            => Assert.Equal(expected, Setting.ResolveSpeed(configured, game));

        [Theory]
        [InlineData(2f, 1f, 2f)]
        [InlineData(1000f, 1f, Setting.MaxMultiplier)]
        [InlineData(0f, 1f, 1f)]
        [InlineData(-1f, 1f, 1f)]
        public void Multiplier_must_be_positive_and_is_capped(float configured, float game, float expected)
            => Assert.Equal(expected, Setting.ResolveMultiplier(configured, game));

        [Theory]
        [InlineData(6, 4, 100, 6)]
        [InlineData(1, 4, 100, 1)]
        [InlineData(500, 4, 100, 100)]      // capped
        [InlineData(0, 4, 100, 4)]          // zero stations is not a setting
        [InlineData(-1, 4, 100, 4)]
        public void Count_is_at_least_one_and_is_capped(int configured, int game, int max, int expected)
            => Assert.Equal(expected, Setting.ResolveCount(configured, game, max));
    }
}
