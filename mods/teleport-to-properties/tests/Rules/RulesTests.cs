using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TeleportToProperties.Tests
{
    public class DestinationsTests
    {
        private record Item(string Code, string Name, bool IsBusiness);

        private static List<Item> Order(params Item[] items)
            => Destinations.Order(items, x => x.Code, x => x.Name, x => x.IsBusiness);

        [Fact]
        public void Lists_properties_before_businesses_and_each_group_by_name()
        {
            List<Item> result = Order(
                new Item("laundromat", "Laundromat", true),
                new Item("rv", "RV", false),
                new Item("bungalow", "bungalow", false),
                new Item("carwash", "Car Wash", true));

            Assert.Equal(new[] { "bungalow", "rv", "carwash", "laundromat" }, result.Select(x => x.Code));
        }

        [Fact]
        public void Keeps_the_first_of_two_items_with_the_same_code()
        {
            // A business is also in the property list: it must show once.
            List<Item> result = Order(new Item("a", "First", true), new Item("a", "Second", false));

            Item only = Assert.Single(result);
            Assert.Equal("First", only.Name);
        }

        [Fact]
        public void Tells_items_with_blank_codes_apart_by_name()
        {
            Assert.Equal(2, Order(new Item("", "One", false), new Item(null, "Two", false)).Count);
            Assert.Single(Order(new Item("", "One", false), new Item(null, "One", false)));
        }

        [Fact]
        public void The_order_does_not_depend_on_the_input_order()
        {
            var a = new Item("a", "Same", false);
            var b = new Item("b", "Same", false);
            Assert.Equal(Order(a, b).Select(x => x.Code), Order(b, a).Select(x => x.Code));
        }

        [Fact]
        public void An_empty_list_stays_empty() => Assert.Empty(Order());
    }

    public class SelectionTests
    {
        private static readonly string[] Codes = { "a", "b", "c" };

        [Theory]
        [InlineData("a", 0)]
        [InlineData("c", 2)]
        [InlineData("gone", 0)]   // the pick was lost: start again from the top
        [InlineData(null, 0)]
        public void Finds_the_remembered_code(string remembered, int expected)
            => Assert.Equal(expected, Selection.Resolve(Codes, remembered));

        [Fact]
        public void An_empty_list_has_no_pick()
        {
            Assert.Equal(-1, Selection.Resolve(new string[0], "a"));
            Assert.Equal(-1, Selection.Resolve(null, "a"));
        }

        [Theory]
        [InlineData(0, 3, 1, 1)]
        [InlineData(2, 3, 1, 0)]    // wraps at the end
        [InlineData(0, 3, -1, 2)]   // and at the start
        [InlineData(0, 1, 1, 0)]
        [InlineData(1, 3, -7, 0)]   // large steps still land inside the list
        [InlineData(0, 0, 1, -1)]
        public void Steps_and_wraps(int current, int count, int delta, int expected)
            => Assert.Equal(expected, Selection.Step(current, count, delta));
    }

    public class GuardTests
    {
        [Theory]
        [InlineData(true, false, true, Refusal.None)]
        [InlineData(false, false, true, Refusal.NotInGame)]
        [InlineData(true, true, true, Refusal.InVehicle)]
        [InlineData(true, false, false, Refusal.NoSpawnPoint)]
        [InlineData(false, true, false, Refusal.NotInGame)]   // the first problem is the one reported
        public void Names_the_first_reason_a_teleport_cannot_happen(bool ready, bool vehicle, bool spawn, Refusal expected)
            => Assert.Equal(expected, Guard.Check(ready, vehicle, spawn));

        [Fact]
        public void Every_refusal_has_text_and_none_has_none()
        {
            Assert.Equal("", Guard.Describe(Refusal.None));
            foreach (Refusal r in new[] { Refusal.NotInGame, Refusal.InVehicle, Refusal.NoSpawnPoint })
                Assert.NotEqual("", Guard.Describe(r));
        }
    }

    public class KeyNamesTests
    {
        public enum Sample { LeftBracket, F6, Backslash }

        [Theory]
        [InlineData("LeftBracket", Sample.LeftBracket)]
        [InlineData("f6", Sample.F6)]
        [InlineData("  Backslash ", Sample.Backslash)]
        [InlineData("left bracket", Sample.LeftBracket)]
        public void Reads_a_key_name(string text, Sample expected)
        {
            Assert.True(KeyNames.TryParse(text, out Sample key));
            Assert.Equal(expected, key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("F6,Backslash")]
        [InlineData("Nope")]
        public void Rejects_anything_else(string text) => Assert.False(KeyNames.TryParse(text, out Sample _));
    }
}
