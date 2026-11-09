using Xunit;

namespace FasterDealerMovement.Tests
{
    public class DealSpeedTests
    {
        [Theory]
        [InlineData(6f)]
        [InlineData(1f)]
        [InlineData(12f)]
        [InlineData(4.9f)]
        public void Keeps_a_value_in_range(float configured)
        {
            Assert.Equal(configured, DealSpeed.Resolve(configured, out bool valid));
            Assert.True(valid);
        }

        [Theory]
        [InlineData(0.5f, 1f)]    // too slow: moved up to the minimum
        [InlineData(100f, 12f)]   // too fast: moved down to the maximum
        public void Moves_a_value_out_of_range_to_the_nearest_end(float configured, float expected)
        {
            Assert.Equal(expected, DealSpeed.Resolve(configured, out bool valid));
            Assert.True(valid);   // usable, just clamped: the config warns by comparing the two values
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(0f)]
        [InlineData(-3f)]
        public void Falls_back_to_the_default_for_an_unusable_value(float configured)
        {
            Assert.Equal(DealSpeed.Default, DealSpeed.Resolve(configured, out bool valid));
            Assert.False(valid);
        }

        [Fact]
        public void Default_is_faster_than_the_games_urgent_and_rain_speeds_and_inside_the_range()
        {
            Assert.True(DealSpeed.Default > 4.9f);   // 0.7 of the 7 m/s maximum
            Assert.True(DealSpeed.Default > 5.4f);   // 1.8 walking speed times the 3x rain multiplier
            Assert.InRange(DealSpeed.Default, DealSpeed.Min, DealSpeed.Max);
        }

        [Fact]
        public void Priority_is_above_the_games_urgent_entry_and_below_its_overrides()
        {
            Assert.True(DealSpeed.Priority > 10);    // urgent contract
            Assert.True(DealSpeed.Priority < 80);    // cowering, flinching, standing up
        }
    }
}
