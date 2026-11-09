using Xunit;

namespace ConfigurableStorage.Tests
{
    public class StorageSizingTests
    {
        [Theory]
        [InlineData(0, 4, 4)]    // unset: the game's size
        [InlineData(-3, 4, 4)]   // unusable: the game's size
        [InlineData(12, 4, 12)]
        [InlineData(20, 8, 20)]
        [InlineData(500, 8, 20)] // capped at the game's maximum
        [InlineData(2, 8, 2)]    // below the game's size is allowed
        [InlineData(1, 8, 1)]
        public void Slots_are_the_game_size_or_the_configured_count_within_one_to_twenty(int configured, int game, int expected)
            => Assert.Equal(expected, StorageSizing.Slots(configured, game));

        [Theory]
        [InlineData(3, 12, 4, 2, 3)]
        [InlineData(9, 12, 4, 2, 5)]
        [InlineData(5, 3, 4, 2, 3)]
        [InlineData(0, 4, 4, 2, 2)]
        [InlineData(0, 6, 4, 2, 3)]    // 4 slots in 2 rows is 2 across; 6 slots keep 2 across: 3 rows
        [InlineData(0, 20, 8, 4, 5)]   // 8 in 4 rows is 2 across; 20 would be 10 rows, capped at 5 (4 across)
        [InlineData(0, 12, 6, 1, 2)]   // 6 in 1 row is 6 across; 12 is 2 rows
        [InlineData(0, 2, 8, 4, 1)]    // shrunk to 2 slots: one row
        public void Rows_keep_the_games_width_and_stay_within_one_to_five(int configured, int slots, int gameSlots, int gameRows, int expected)
            => Assert.Equal(expected, StorageSizing.Rows(configured, slots, gameSlots, gameRows));

        [Fact]
        public void Every_container_has_a_distinct_id()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var (id, name) in Containers.All)
            {
                Assert.True(seen.Add(id), id);
                Assert.False(string.IsNullOrWhiteSpace(name));
            }
            Assert.Equal(8, Containers.All.Length);
        }
    }
}
