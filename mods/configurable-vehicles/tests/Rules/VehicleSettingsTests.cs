using Xunit;

namespace ConfigurableVehicles.Tests
{
    public class PositiveTests
    {
        [Theory]
        [InlineData(1600f, 20000f, 1600f)]
        [InlineData(0f, 20000f, 0f)]                       // 0 = the game's value
        [InlineData(-5f, 20000f, 0f)]
        [InlineData(float.NaN, 20000f, 0f)]
        [InlineData(float.PositiveInfinity, 20000f, 0f)]
        [InlineData(1000000f, 20000f, 20000f)]              // a typo is capped, not obeyed
        public void Unusable_is_zero_and_huge_is_capped(float configured, float max, float expected)
            => Assert.Equal(expected, VehicleSettings.Positive(configured, max));
    }

    public class SlotTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(-4, 0)]
        [InlineData(12, 12)]
        [InlineData(99, VehicleSettings.MaxSlots)]
        public void Slots_are_clamped_and_zero_means_the_game(int configured, int expected)
            => Assert.Equal(expected, VehicleSettings.Slots(configured));

        [Theory]
        [InlineData("20", 20)]
        [InlineData(" 8 ", 8)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("lots", 0)]
        [InlineData("veeper=20", 0)]                         // the old per-vehicle text format is not a number
        [InlineData("500", VehicleSettings.MaxSlots)]
        public void The_all_vehicles_text(string text, int expected)
            => Assert.Equal(expected, VehicleSettings.SlotsFromText(text));

        [Theory]
        [InlineData(8, 20, 8)]    // a vehicle's own setting wins
        [InlineData(0, 20, 20)]
        [InlineData(0, 0, 0)]
        public void A_vehicles_own_size_beats_the_all_vehicles_one(int own, int all, int expected)
            => Assert.Equal(expected, VehicleSettings.SlotsFor(own, all));
    }

    public class RowsForTests
    {
        [Theory]
        [InlineData(2, 20, 2)]
        [InlineData(2, 1, 1)]     // never more rows than slots
        [InlineData(9, 20, 5)]    // the game's maximum
        [InlineData(0, 20, 1)]
        [InlineData(-3, 20, 1)]
        public void Uses_the_configured_rows_within_the_games_range(int configured, int slots, int expected)
            => Assert.Equal(expected, VehicleSettings.RowsFor(configured, slots));
    }

    public class VehicleOverridesTests
    {
        [Fact]
        public void Nothing_set_means_no_handling_change()
            => Assert.True(new VehicleOverrides(0, 0, 0, 0, 0, 0, 0, 0, 0).HasNoHandling);

        [Fact]
        public void Any_value_set_counts_as_a_change()
        {
            Assert.False(new VehicleOverrides(1600, 0, 0, 0, 0, 0, 0, 0, 0).HasNoHandling);
            Assert.False(new VehicleOverrides(0, 0, 0, 0, 0, 0, 0, 1.2f, 0).HasNoHandling);
        }

        [Fact]
        public void A_trunk_size_alone_is_not_a_handling_change()
        {
            var overrides = new VehicleOverrides(0, 0, 0, 0, 0, 0, 0, 0, 12);
            Assert.True(overrides.HasNoHandling);
            Assert.Equal(12, overrides.TrunkSlots);
        }

        [Fact]
        public void Values_are_cleaned_and_capped()
        {
            var overrides = new VehicleOverrides(-1, float.NaN, 9999, 1000, 0, 50, 0, 0, 99);

            Assert.Equal(0f, overrides.Mass);
            Assert.Equal(0f, overrides.Power);
            Assert.Equal(VehicleOverrides.MaxSpeed, overrides.TopSpeed);
            Assert.Equal(VehicleOverrides.MaxAngle, overrides.SteerAngle);
            Assert.Equal(VehicleOverrides.MaxMultiplier, overrides.Brake);
            Assert.Equal(VehicleSettings.MaxSlots, overrides.TrunkSlots);
        }
    }
}
