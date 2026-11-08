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

        [Theory]
        [InlineData(7, 5, 5)]      // more asked for than the screen has rows: held to the rows
        [InlineData(7, 7, 7)]
        [InlineData(3, 5, 3)]      // fewer is always fine
        [InlineData(7, 0, 7)]      // screen not started yet: nothing to hold it to
        [InlineData(20, 0, 20)]
        public void The_limit_never_exceeds_the_rows_the_screen_has(int wanted, int rows, int expected)
            => Assert.Equal(expected, RouteRows.Limit(wanted, rows));

        // The viewport spans -50..50 in these.
        [Theory]
        [InlineData(-10f, 10f, 0f)]       // fully inside: stay put
        [InlineData(-50f, 50f, 0f)]       // exactly filling it
        [InlineData(40f, 70f, 20f)]       // sticks out of the top: content moves down by 20
        [InlineData(-90f, -60f, -40f)]    // below the bottom: content moves up by 40
        [InlineData(-80f, 80f, 30f)]      // taller than the view: align the top
        public void Scroll_delta_brings_a_row_into_view(float childMin, float childMax, float expected)
            => Assert.Equal(expected, RouteRows.ScrollDelta(childMin, childMax, -50f, 50f));
    }
}
