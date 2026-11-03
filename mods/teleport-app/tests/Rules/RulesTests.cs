using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TeleportApp.Tests
{
    public class DestinationsTests
    {
        private record Item(string Code, string Name, int Group);

        private static List<Item> Order(params Item[] items)
            => Destinations.Order(items, x => x.Code, x => x.Name, x => x.Group);

        [Fact]
        public void Lists_by_group_and_each_group_by_name()
        {
            List<Item> result = Order(
                new Item("benji", "Benji", 2),
                new Item("laundromat", "Laundromat", 1),
                new Item("rv", "RV", 0),
                new Item("bungalow", "bungalow", 0),
                new Item("carwash", "Car Wash", 1),
                new Item("brad", "Brad", 2));

            Assert.Equal(new[] { "bungalow", "rv", "carwash", "laundromat", "benji", "brad" }, result.Select(x => x.Code));
        }

        [Fact]
        public void Keeps_the_first_of_two_items_with_the_same_code()
        {
            // A business is also in the property list: it must show once.
            List<Item> result = Order(new Item("a", "First", 1), new Item("a", "Second", 0));

            Item only = Assert.Single(result);
            Assert.Equal("First", only.Name);
        }

        [Fact]
        public void Tells_items_with_blank_codes_apart_by_name()
        {
            Assert.Equal(2, Order(new Item("", "One", 0), new Item(null, "Two", 0)).Count);
            Assert.Single(Order(new Item("", "One", 0), new Item(null, "One", 0)));
        }

        [Fact]
        public void The_order_does_not_depend_on_the_input_order()
        {
            var a = new Item("a", "Same", 0);
            var b = new Item("b", "Same", 0);
            Assert.Equal(Order(a, b).Select(x => x.Code), Order(b, a).Select(x => x.Code));
        }

        [Fact]
        public void An_empty_list_stays_empty() => Assert.Empty(Order());
    }

    public class CategoriesTests
    {
        [Fact]
        public void Offers_only_kinds_that_have_places_in_list_order()
        {
            Assert.Equal(new[] { PlaceKind.Property, PlaceKind.Dealer }, Categories.NonEmpty(new[] { 2, 0, 1 }));
            Assert.Equal(new[] { PlaceKind.Business }, Categories.NonEmpty(new[] { 0, 4, 0 }));
            Assert.Empty(Categories.NonEmpty(new[] { 0, 0, 0, 0 }));
            Assert.Equal(new[] { PlaceKind.Store }, Categories.NonEmpty(new[] { 0, 0, 0, 3 }));
        }

        [Fact]
        public void Has_a_count_slot_for_every_kind()
            => Assert.Equal(System.Enum.GetValues(typeof(PlaceKind)).Length, Categories.Count);

        [Theory]
        [InlineData(PlaceKind.Property, 3, "Properties (3)")]
        [InlineData(PlaceKind.Business, 1, "Businesses (1)")]
        [InlineData(PlaceKind.Dealer, 6, "Dealers (6)")]
        [InlineData(PlaceKind.Store, 10, "Stores (10)")]
        public void Labels_a_category_with_its_count(PlaceKind kind, int count, string expected)
            => Assert.Equal(expected, Categories.Label(kind, count));
    }

    public class FilterTests
    {
        [Theory]
        [InlineData(true, true, true, true)]     // on, owned
        [InlineData(true, true, false, false)]   // on, but only owned ones, and this is not
        [InlineData(true, false, false, true)]   // on, owned or not
        [InlineData(false, false, true, false)]  // the whole kind is off
        public void Offers_a_place_when_the_kind_is_on_and_the_owned_rule_allows_it(bool kindOn, bool onlyOwned, bool owned, bool expected)
            => Assert.Equal(expected, Filter.Offers(kindOn, onlyOwned, owned));
    }

    public class StoresTests
    {
        [Fact]
        public void The_table_has_distinct_named_finite_points()
        {
            Assert.NotEmpty(Stores.All);
            Assert.Equal(Stores.All.Length, Stores.All.Select(s => s.Name).Distinct().Count());
            foreach (Stores.Store s in Stores.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Name));
                Assert.True(float.IsFinite(s.X) && float.IsFinite(s.Y) && float.IsFinite(s.Z));
            }
        }
    }

    public class RotationTests
    {
        [Fact]
        public void Blank_kinds_mean_every_kind_and_none_means_no_kind()
        {
            Assert.Equal(4, Rotation.ParseKinds("").Count);
            Assert.Equal(4, Rotation.ParseKinds(null).Count);
            Assert.Empty(Rotation.ParseKinds("None"));
        }

        [Fact]
        public void Kinds_round_trip_in_a_fixed_order_and_ignore_unknown_names()
        {
            string text = Rotation.JoinKinds(new[] { PlaceKind.Store, PlaceKind.Property, PlaceKind.Store });
            Assert.Equal("Property|Store", text);
            Assert.Equal(new[] { PlaceKind.Property, PlaceKind.Store }, Rotation.ParseKinds(text).OrderBy(k => (int)k));
            Assert.Equal(new[] { PlaceKind.Dealer }, Rotation.ParseKinds("dealer|Nope|99|").ToArray());
            Assert.Equal("None", Rotation.JoinKinds(new PlaceKind[0]));
        }

        [Fact]
        public void Codes_round_trip_sorted_and_skip_blanks()
        {
            Assert.Equal("a|b|store:Pawn Shop", Rotation.JoinCodes(new[] { "store:Pawn Shop", "b", "a" }));
            HashSet<string> codes = Rotation.ParseCodes(" b | a ||store:Pawn Shop ");
            Assert.Equal(new[] { "a", "b", "store:Pawn Shop" }, codes.OrderBy(c => c));
            Assert.Empty(Rotation.ParseCodes(""));
        }

        [Theory]
        [InlineData(true, false, true)]    // kind on, place not left out
        [InlineData(true, true, false)]    // kind on, place left out
        [InlineData(false, false, false)]  // the kind is out
        public void A_place_is_in_the_rotation_when_its_kind_is_and_it_was_not_left_out(bool kindOn, bool leftOut, bool expected)
        {
            var kinds = kindOn ? new HashSet<PlaceKind> { PlaceKind.Dealer } : new HashSet<PlaceKind>();
            var excluded = leftOut ? new HashSet<string> { "benji" } : new HashSet<string>();
            Assert.Equal(expected, Rotation.Includes(kinds, excluded, PlaceKind.Dealer, "benji"));
        }
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
