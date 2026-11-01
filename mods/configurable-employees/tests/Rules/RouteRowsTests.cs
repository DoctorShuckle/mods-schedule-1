using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class RouteRowsTests
    {
        [Theory]
        [InlineData(10, 5, 5)]
        [InlineData(5, 5, 0)]       // the game's own count: UI untouched
        [InlineData(3, 5, 0)]
        [InlineData(-1, 5, 0)]      // unset
        [InlineData(500, 5, RouteRows.Max - 5)]   // capped
        public void Route_rows_to_add(int wanted, int have, int expected)
            => Assert.Equal(expected, RouteRows.RowsToAdd(wanted, have));
    }
}
