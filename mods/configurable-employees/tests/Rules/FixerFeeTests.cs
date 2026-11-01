using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class FixerFeeTests
    {
        // The game: 100 for each of the first six hires, 250 for the rest, at most 500.
        private static float Game(int hires) => FixerFee.AdditionalSigningFee(hires, 100f, 6, 250f, 500f);

        [Theory]
        [InlineData(0, 0f)]
        [InlineData(1, 100f)]
        [InlineData(5, 500f)]
        [InlineData(6, 500f)]    // 600 uncapped
        [InlineData(20, 500f)]
        public void Defaults_reproduce_the_games_fee(int hires, float expected)
            => Assert.Equal(expected, Game(hires));

        [Fact]
        public void The_original_formula_gives_the_same_numbers()
        {
            // The game's own loop, written out, compared against every hire count that matters.
            for (int hires = 0; hires < 50; hires++)
            {
                float expected = 0f;
                for (int i = 0; i < hires; i++) expected += i > 5 ? 250f : 100f;
                Assert.Equal(System.Math.Min(expected, 500f), Game(hires));
            }
        }

        [Fact]
        public void Tiers_and_cap_can_be_changed()
        {
            // 10 per hire for the first two, 50 afterwards, capped at 120: 10+10+50+50 = 120.
            Assert.Equal(120f, FixerFee.AdditionalSigningFee(4, 10f, 2, 50f, 120f));
            Assert.Equal(120f, FixerFee.AdditionalSigningFee(9, 10f, 2, 50f, 120f));
            Assert.Equal(20f, FixerFee.AdditionalSigningFee(2, 10f, 2, 50f, 120f));
        }

        [Fact]
        public void Zero_makes_hiring_free_of_the_extra_fee()
            => Assert.Equal(0f, FixerFee.AdditionalSigningFee(10, 0f, 6, 0f, 500f));

        [Fact]
        public void Unusable_values_fall_back_to_the_games()
            => Assert.Equal(Game(8), FixerFee.AdditionalSigningFee(8, -5f, -3, float.NaN, -1f));

        [Fact]
        public void Vanilla_settings_are_recognised_so_the_game_result_is_left_alone()
        {
            Assert.True(FixerFee.IsVanillaFee(100f, 6, 250f, 500f));
            Assert.True(FixerFee.IsVanillaFee(-1f, -1, -1f, -1f));
            Assert.False(FixerFee.IsVanillaFee(100f, 6, 250f, 400f));
            Assert.False(FixerFee.IsVanillaFee(100f, 3, 250f, 500f));
        }
    }
}
