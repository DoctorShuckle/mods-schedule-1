using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PersistentPotAdditives.Tests
{
    public class ShouldCaptureTests
    {
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]   // the game's handler is a no-op without a plant
        [InlineData(false, true, false)]   // clients stay out of it; the host broadcasts
        [InlineData(false, false, false)]
        public void Captures_only_on_the_host_with_a_plant(bool isServer, bool hasPlant, bool expected)
            => Assert.Equal(expected, Decisions.ShouldCapture(isServer, hasPlant));
    }

    public class ShouldRestoreTests
    {
        [Theory]
        [InlineData(1, 1, true)]    // last use left for the next plant: still restore
        [InlineData(3, 2, true)]
        [InlineData(2, 0, false)]   // soil spent: the game clears it, additives end
        [InlineData(2, -1, false)]
        [InlineData(0, 3, false)]   // nothing was on the pot
        [InlineData(0, 0, false)]
        public void Restores_only_when_something_was_captured_and_soil_remains(int captured, int usesLeft, bool expected)
            => Assert.Equal(expected, Decisions.ShouldRestore(captured, usesLeft));
    }

    public class IdsToRestoreTests
    {
        [Fact]
        public void Returns_every_captured_id_in_order_when_none_are_applied()
        {
            var ids = Decisions.IdsToRestore(new[] { "speedgrow", "fertilizer", "pgr" }, _ => false);
            Assert.Equal(new[] { "speedgrow", "fertilizer", "pgr" }, ids);
        }

        [Fact]
        public void Skips_ids_already_on_the_pot()
        {
            var onPot = new HashSet<string> { "fertilizer" };
            var ids = Decisions.IdsToRestore(new[] { "speedgrow", "fertilizer", "pgr" }, onPot.Contains);
            Assert.Equal(new[] { "speedgrow", "pgr" }, ids);
        }

        [Fact]
        public void Never_returns_the_same_id_twice()
        {
            var ids = Decisions.IdsToRestore(new[] { "pgr", "pgr", "speedgrow", "pgr" }, _ => false);
            Assert.Equal(new[] { "pgr", "speedgrow" }, ids);
        }

        [Fact]
        public void Asks_whether_applied_as_each_id_is_reached_so_earlier_applies_count()
        {
            // Mirrors the patch loop: applying an id puts it on the pot before the next is checked.
            var onPot = new HashSet<string>();
            var applied = new List<string>();
            foreach (string id in Decisions.IdsToRestore(new[] { "a", "b" }, onPot.Contains))
            {
                applied.Add(id);
                onPot.Add(id);
                onPot.Add("b"); // applying "a" happens to bring "b" back too
            }
            Assert.Equal(new[] { "a" }, applied);
        }

        [Fact]
        public void Returns_nothing_for_an_empty_capture()
            => Assert.Empty(Decisions.IdsToRestore(Enumerable.Empty<string>(), _ => false));
    }

    public class ShouldBoostTests
    {
        // Baseline is a fresh planting on the host that should boost; each test changes one input.
        private static bool Decide(bool isServer = true, bool loading = false, float progress = 0f,
                                   bool hasPlant = true, float boost = 0.25f)
            => Decisions.ShouldBoost(isServer, loading, progress, hasPlant, boost);

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
        public void Is_zero_for_no_additives() => Assert.Equal(0f, Decisions.TotalGrowth(new float[0]));

        [Fact]
        public void Sums_the_boosts() => Assert.Equal(0.45f, Decisions.TotalGrowth(new[] { 0.25f, 0.2f }), 5);

        [Fact]
        public void Ignores_additives_with_no_instant_growth()
            => Assert.Equal(0.25f, Decisions.TotalGrowth(new[] { 0f, 0.25f, 0f }), 5);

        [Fact]
        public void Ignores_negative_values() => Assert.Equal(0.25f, Decisions.TotalGrowth(new[] { -1f, 0.25f }), 5);
    }
}
