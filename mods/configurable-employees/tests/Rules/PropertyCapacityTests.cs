using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class PropertyCapacityTests
    {
        // The catalog is shared static state, so each test puts back what it changed.
        private static void With(PropertyEntry entry, int configured, System.Action check)
        {
            int before = entry.Configured;
            entry.Configured = configured;
            try { check(); }
            finally { entry.Configured = before; }
        }

        [Fact]
        public void An_unset_property_keeps_the_games_capacity()
            => Assert.Equal(10, PropertyCapacity.Resolve("barn", 10));

        [Fact]
        public void A_configured_property_uses_the_setting()
            => With(PropertyCatalog.Barn, 15, () => Assert.Equal(15, PropertyCapacity.Resolve("barn", 10)));

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public void A_setting_below_one_means_unset(int configured)
            => With(PropertyCatalog.Barn, configured, () => Assert.Equal(10, PropertyCapacity.Resolve("barn", 10)));

        [Fact]
        public void The_setting_is_capped()
            => With(PropertyCatalog.Barn, 5000, () => Assert.Equal(PropertyCapacity.Max, PropertyCapacity.Resolve("barn", 10)));

        [Fact]
        public void The_code_is_matched_without_regard_to_case()
            => With(PropertyCatalog.DocksWarehouse, 20, () => Assert.Equal(20, PropertyCapacity.Resolve("DocksWarehouse", 12)));

        [Theory]
        [InlineData("motelroom")]    // not offered by the Fixer, so no setting
        [InlineData("rv")]
        [InlineData("carwash")]      // a business
        [InlineData("")]
        [InlineData(null)]
        public void A_property_without_a_setting_keeps_the_games_capacity(string code)
            => Assert.Equal(7, PropertyCapacity.Resolve(code, 7));

        [Theory]
        [InlineData(10, 8, 2)]
        [InlineData(8, 8, 0)]
        [InlineData(5, 8, 0)]      // never removes anything
        [InlineData(0, 8, 0)]
        public void Idle_points_to_add(int capacity, int have, int expected)
            => Assert.Equal(expected, PropertyCapacity.IdlePointsToAdd(capacity, have));
    }
}
