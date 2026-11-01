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
            => Assert.Equal(expected, HarvestRestore.ShouldCapture(isServer, hasPlant));
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
            => Assert.Equal(expected, HarvestRestore.ShouldRestore(captured, usesLeft));
    }

    public class IdsToRestoreTests
    {
        [Fact]
        public void Returns_every_captured_id_in_order_when_none_are_applied()
        {
            var ids = HarvestRestore.IdsToRestore(new[] { "speedgrow", "fertilizer", "pgr" }, _ => false);
            Assert.Equal(new[] { "speedgrow", "fertilizer", "pgr" }, ids);
        }

        [Fact]
        public void Skips_ids_already_on_the_pot()
        {
            var onPot = new HashSet<string> { "fertilizer" };
            var ids = HarvestRestore.IdsToRestore(new[] { "speedgrow", "fertilizer", "pgr" }, onPot.Contains);
            Assert.Equal(new[] { "speedgrow", "pgr" }, ids);
        }

        [Fact]
        public void Never_returns_the_same_id_twice()
        {
            var ids = HarvestRestore.IdsToRestore(new[] { "pgr", "pgr", "speedgrow", "pgr" }, _ => false);
            Assert.Equal(new[] { "pgr", "speedgrow" }, ids);
        }

        [Fact]
        public void Asks_whether_applied_as_each_id_is_reached_so_earlier_applies_count()
        {
            // Mirrors the patch loop: applying an id puts it on the pot before the next is checked.
            var onPot = new HashSet<string>();
            var applied = new List<string>();
            foreach (string id in HarvestRestore.IdsToRestore(new[] { "a", "b" }, onPot.Contains))
            {
                applied.Add(id);
                onPot.Add(id);
                onPot.Add("b"); // applying "a" happens to bring "b" back too
            }
            Assert.Equal(new[] { "a" }, applied);
        }

        [Fact]
        public void Returns_nothing_for_an_empty_capture()
            => Assert.Empty(HarvestRestore.IdsToRestore(Enumerable.Empty<string>(), _ => false));
    }
}
