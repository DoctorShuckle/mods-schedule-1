using Xunit;

namespace MixPackagedDrugs.Tests
{
    public class UnitsTests
    {
        [Theory]
        [InlineData(1, 1, 1)]     // one baggie
        [InlineData(3, 5, 15)]    // three jars
        [InlineData(1, 20, 20)]   // one brick: needs 20 mixers
        [InlineData(2, 20, 40)]
        [InlineData(0, 20, 0)]
        [InlineData(-1, 20, 0)]
        [InlineData(4, 0, 4)]     // an unpackaged item counts as one unit
        public void Multiplies_the_stack_by_the_package_size(int quantity, int amount, int expected)
            => Assert.Equal(expected, Packages.Units(quantity, amount));
    }

    public class PackagesForRoomTests
    {
        [Theory]
        [InlineData(20, 20, 1)]    // an empty slot takes a brick
        [InlineData(19, 20, 0)]    // the brick is refused, not split
        [InlineData(20, 5, 4)]
        [InlineData(8, 5, 1)]
        [InlineData(0, 1, 0)]
        [InlineData(-5, 1, 0)]     // an overfull slot takes nothing
        [InlineData(10, 20, 0)]    // a brick never fits a limit of 10
        [InlineData(20, 0, 20)]
        public void Counts_whole_packages_only(int room, int amount, int expected)
            => Assert.Equal(expected, Packages.PackagesForRoom(room, amount));
    }

    public class PlanDropTests
    {
        [Theory]
        [InlineData(false, 1, 1, Packages.DropAction.Allow, 1)]    // an empty slot, a brick that fits
        [InlineData(false, 4, 2, Packages.DropAction.Allow, 2)]
        [InlineData(false, 1, 3, Packages.DropAction.Cut, 1)]      // three bricks dragged, room for one
        [InlineData(false, 0, 1, Packages.DropAction.Cancel, 0)]
        [InlineData(true, 4, 2, Packages.DropAction.Merge, 2)]     // onto a loose stack: units are added
        [InlineData(true, 1, 3, Packages.DropAction.Merge, 1)]     // only what fits
        [InlineData(true, 0, 3, Packages.DropAction.Cancel, 0)]    // full, or a different product
        [InlineData(true, 2, 0, Packages.DropAction.Cancel, 0)]
        [InlineData(false, -1, 2, Packages.DropAction.Cancel, 0)]
        public void Decides_cut_merge_or_cancel(bool hasStack, int fit, int dragging, Packages.DropAction action, int count)
        {
            Assert.Equal(action, Packages.PlanDrop(hasStack, fit, dragging, out int n));
            Assert.Equal(count, n);
        }
    }
}
