using Xunit;

namespace WarehouseAccess.Tests
{
    public class ShouldOpenTests
    {
        [Theory]
        [InlineData(true, true, false, true)]    // always open needs nothing else
        [InlineData(true, false, true, true)]    // or the rank
        [InlineData(true, false, false, false)]  // neither: stays as in the game
        [InlineData(false, true, true, false)]   // the quest gate comes first
        [InlineData(false, false, true, false)]
        [InlineData(false, true, false, false)]
        public void Needs_the_quest_gate_and_then_always_open_or_the_rank(bool quest, bool always, bool rank, bool expected)
            => Assert.Equal(expected, Access.ShouldOpen(quest, always, rank));
    }

    public class TryParseRankTests
    {
        public enum Sample { Street_Rat, Hoodlum, Block_Boss }

        [Theory]
        [InlineData("Hoodlum", Sample.Hoodlum)]
        [InlineData("hoodlum", Sample.Hoodlum)]
        [InlineData("Block_Boss", Sample.Block_Boss)]
        [InlineData("block boss", Sample.Block_Boss)]   // spaces stand for underscores
        [InlineData("  Street_Rat  ", Sample.Street_Rat)]
        public void Reads_a_rank_name(string text, Sample expected)
        {
            Assert.True(Access.TryParseRank(text, out Sample rank));
            Assert.Equal(expected, rank);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Kingpinn")]
        [InlineData("1")]        // a number is not a name, even though Enum.TryParse allows it
        [InlineData("-1")]
        [InlineData("0,1")]
        public void Rejects_anything_else(string text)
            => Assert.False(Access.TryParseRank(text, out Sample _));
    }

    public class TierTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        [InlineData(0, 1)]
        [InlineData(-3, 1)]
        [InlineData(9, 5)]
        public void Stays_within_one_to_five(int configured, int expected)
            => Assert.Equal(expected, Access.Tier(configured));
    }
}
