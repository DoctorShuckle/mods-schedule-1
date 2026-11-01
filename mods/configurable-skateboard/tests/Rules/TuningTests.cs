using Xunit;

namespace ConfigurableSkateboard.Tests
{
    public class MultiplierTests
    {
        [Theory]
        [InlineData(1.5f, 1.5f)]
        [InlineData(1f, 1f)]
        [InlineData(0.25f, 0.25f)]
        [InlineData(1000f, Tuning.MaxMultiplier)]   // capped, not rejected
        [InlineData(0f, 1f)]                         // a board with no speed or jump is not a setting
        [InlineData(-2f, 1f)]
        [InlineData(float.NaN, 1f)]
        [InlineData(float.PositiveInfinity, 1f)]
        public void Must_be_positive_and_is_capped(float configured, float expected)
            => Assert.Equal(expected, Tuning.Multiplier(configured));
    }

    public class CostMultiplierTests
    {
        [Theory]
        [InlineData(0.5f, 0.5f)]
        [InlineData(0f, 0f)]                          // free is valid
        [InlineData(1000f, Tuning.MaxMultiplier)]
        [InlineData(-1f, 1f)]
        [InlineData(float.NaN, 1f)]
        [InlineData(float.NegativeInfinity, 1f)]
        public void Allows_zero_and_is_capped(float configured, float expected)
            => Assert.Equal(expected, Tuning.CostMultiplier(configured));
    }

    public class StaminaChangeTests
    {
        [Fact]
        public void Scales_spending_while_riding() => Assert.Equal(-1.25f, Tuning.StaminaChange(-2.5f, 0.5f, riding: true));

        [Fact]
        public void Makes_spending_free_at_zero() => Assert.Equal(0f, Tuning.StaminaChange(-2.5f, 0f, riding: true));

        [Fact]
        public void Never_scales_regaining_stamina() => Assert.Equal(1f, Tuning.StaminaChange(1f, 0.5f, riding: true));

        [Fact]
        public void Leaves_spending_off_the_board_alone() => Assert.Equal(-2.5f, Tuning.StaminaChange(-2.5f, 0.5f, riding: false));
    }
}
