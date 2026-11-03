using Xunit;

namespace BrickMixer.Tests
{
    public class IsBrickTests
    {
        [Theory]
        [InlineData(20, true)]
        [InlineData(5, false)]
        [InlineData(1, false)]
        [InlineData(0, false)]
        [InlineData(21, false)]
        public void Only_twenty_units_is_a_brick(int units, bool expected)
            => Assert.Equal(expected, BrickRules.IsBrick(units));
    }

    public class BatchReadyTests
    {
        [Theory]
        [InlineData(20, 20, true)]
        [InlineData(20, 19, false)]   // not enough mixers
        [InlineData(19, 20, false)]
        [InlineData(40, 25, true)]
        [InlineData(0, 20, false)]
        public void Needs_a_full_brick_and_a_full_brick_of_mixers(int product, int mixer, bool expected)
            => Assert.Equal(expected, BrickRules.BatchReady(product, mixer));
    }

    public class BricksThatFitTests
    {
        [Theory]
        [InlineData(20, 0, 1)]
        [InlineData(20, 1, 0)]    // 19 units of room: refused, never split
        [InlineData(40, 0, 2)]
        [InlineData(40, 10, 1)]
        [InlineData(20, 20, 0)]
        [InlineData(20, 30, 0)]   // an overfull slot takes nothing
        [InlineData(10, 0, 0)]    // a brick never fits a limit of 10
        public void Counts_whole_bricks_only(int limit, int current, int expected)
            => Assert.Equal(expected, BrickRules.BricksThatFit(limit, current));
    }

    public class RepackBricksTests
    {
        [Theory]
        [InlineData(20, 1)]
        [InlineData(40, 2)]
        [InlineData(10, 0)]
        [InlineData(0, 0)]
        [InlineData(-20, 0)]
        [InlineData(30, 0)]
        public void A_batch_is_bricks_only_when_it_divides_evenly(int units, int expected)
            => Assert.Equal(expected, BrickRules.RepackBricks(units));
    }

    public class PriceTests
    {
        [Theory]
        [InlineData(1000f, 1.5f, 1500f)]
        [InlineData(1000f, 1f, 1000f)]
        [InlineData(333.33f, 1.5f, 500f)]    // rounded to cents
        [InlineData(-5f, 2f, 0f)]
        [InlineData(1000f, -1f, 0f)]
        public void Is_the_mk2_price_times_the_multiplier(float mk2, float multiplier, float expected)
            => Assert.Equal(expected, BrickRules.Price(mk2, multiplier), 1);

        [Fact]
        public void Not_a_number_costs_nothing()
            => Assert.Equal(0f, BrickRules.Price(float.NaN, 2f));

        [Theory]
        [InlineData(2f, 2f)]
        [InlineData(1000f, 1000f)]
        [InlineData(0f, 1.5f)]
        [InlineData(-1f, 1.5f)]
        [InlineData(1001f, 1.5f)]
        [InlineData(float.NaN, 1.5f)]
        [InlineData(float.PositiveInfinity, 1.5f)]
        public void A_multiplier_must_be_above_zero_and_at_most_a_thousand(float value, float expected)
            => Assert.Equal(expected, BrickRules.ValidMultiplier(value, 1.5f));
    }

    public class TintTests
    {
        [Theory]
        [InlineData("#FF0000", 1f, 0f, 0f)]
        [InlineData("00ff00", 0f, 1f, 0f)]
        [InlineData("  #0000Ff ", 0f, 0f, 1f)]
        [InlineData("#E02626", 0.8784f, 0.1490f, 0.1490f)]
        public void Reads_a_hex_color(string text, float r, float g, float b)
        {
            Assert.True(TintRules.TryParseHex(text, out float rr, out float gg, out float bb));
            Assert.Equal(r, rr, 3);
            Assert.Equal(g, gg, 3);
            Assert.Equal(b, bb, 3);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("red")]
        [InlineData("#FFF")]
        [InlineData("#GG0000")]
        [InlineData("#FF00000")]
        public void Rejects_anything_else(string text)
            => Assert.False(TintRules.TryParseHex(text, out _, out _, out _));

        [Fact]
        public void The_default_color_reads()
            => Assert.True(TintRules.TryParseHex(TintRules.DefaultHex, out _, out _, out _));

        [Theory]
        [InlineData(0.9f, 0f, 1f)]      // no strength leaves the color alone
        [InlineData(0.2f, 1f, 0.2f)]    // full strength is the tint
        [InlineData(0.2f, 0.5f, 0.6f)]
        [InlineData(1f, 0.7f, 1f)]
        public void Blends_between_unchanged_and_the_tint(float tint, float strength, float expected)
            => Assert.Equal(expected, TintRules.Factor(tint, strength), 3);

        [Theory]
        [InlineData(0f, 1f, 0f)]
        [InlineData(1f, 0.5f, 1f)]
        [InlineData(0.5f, 0.2f, 0.5f)]
        [InlineData(-0.1f, 1f, 1f)]
        [InlineData(1.1f, 1f, 1f)]
        [InlineData(float.NaN, 1f, 1f)]
        public void A_strength_is_between_zero_and_one(float value, float fallback, float expected)
            => Assert.Equal(expected, TintRules.ValidStrength(value, fallback));
    }
}
