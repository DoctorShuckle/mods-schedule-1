using Xunit;

namespace PersistentPotAdditives.Tests
{
    public class ShouldBoostTests
    {
        // Baseline is a fresh planting on the host that should boost; each test changes one input.
        private static bool Decide(bool isServer = true, bool loading = false, float progress = 0f,
                                   bool hasPlant = true, float boost = 0.25f)
            => ReplantBoost.ShouldBoost(isServer, loading, progress, hasPlant, boost);

        [Fact]
        public void Boosts_a_fresh_planting_on_the_host() => Assert.True(Decide());

        [Fact]
        public void Does_not_boost_on_clients() => Assert.False(Decide(isServer: false));

        [Fact]
        public void Does_not_boost_plants_restored_while_a_save_loads() => Assert.False(Decide(loading: true));

        [Theory]
        [InlineData(0.01f)]
        [InlineData(0.5f)]
        [InlineData(1f)]
        public void Does_not_boost_a_plant_that_already_has_progress(float progress)
            => Assert.False(Decide(progress: progress));

        [Fact]
        public void Does_not_boost_when_the_game_refused_the_seed() => Assert.False(Decide(hasPlant: false));

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.1f)]
        public void Does_not_boost_without_a_growth_additive(float boost) => Assert.False(Decide(boost: boost));

        [Fact]
        public void Loading_a_save_blocks_the_boost_even_if_progress_reads_zero()
            => Assert.False(Decide(loading: true, progress: 0f));
    }

    public class TotalGrowthTests
    {
        [Fact]
        public void Is_zero_for_no_additives() => Assert.Equal(0f, ReplantBoost.TotalGrowth(new float[0]));

        [Fact]
        public void Sums_the_boosts() => Assert.Equal(0.45f, ReplantBoost.TotalGrowth(new[] { 0.25f, 0.2f }), 5);

        [Fact]
        public void Ignores_additives_with_no_instant_growth()
            => Assert.Equal(0.25f, ReplantBoost.TotalGrowth(new[] { 0f, 0.25f, 0f }), 5);

        [Fact]
        public void Ignores_negative_values() => Assert.Equal(0.25f, ReplantBoost.TotalGrowth(new[] { -1f, 0.25f }), 5);
    }
}
