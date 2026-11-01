using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class LogLevelTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 2)]
        [InlineData(100, 2)]
        [InlineData(-1, 0)]
        public void Out_of_range_levels_are_pulled_back_into_range(int configured, int expected)
            => Assert.Equal(expected, LogLevel.Clamp(configured));
    }
}
