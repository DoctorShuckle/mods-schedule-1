using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class PropertyCatalogTests
    {
        [Fact]
        public void Codes_and_keys_are_unique()
        {
            var codes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var keys = new HashSet<string>();
            foreach (PropertyEntry entry in PropertyCatalog.All)
            {
                Assert.True(codes.Add(entry.Code), entry.Code + " is listed twice");
                Assert.True(keys.Add(entry.Key), entry.Key + " is listed twice");
            }
        }

        [Fact]
        public void Entries_start_unset()
        {
            foreach (PropertyEntry entry in PropertyCatalog.All) Assert.Equal(-1, entry.Configured);
        }

        [Fact]
        public void Find_matches_the_code_without_regard_to_case()
        {
            Assert.Same(PropertyCatalog.StorageUnit, PropertyCatalog.Find("storageunit"));
            Assert.Same(PropertyCatalog.StorageUnit, PropertyCatalog.Find("StorageUnit"));
            Assert.Null(PropertyCatalog.Find("nowhere"));
            Assert.Null(PropertyCatalog.Find(null));
        }

        [Fact]
        public void Every_entry_has_a_game_capacity_to_show()
        {
            // A property with no capacity cannot hold employees, so it has no business being here.
            foreach (PropertyEntry entry in PropertyCatalog.All) Assert.True(entry.GameCapacity > 0, entry.Code);
        }

        [Fact]
        public void Properties_that_cannot_hold_employees_have_no_entry()
        {
            foreach (string code in new[] { "seweroffice", "rv", "motelroom", "carwash", "laundromat", "postoffice", "tacoticklers", "hylandmedical" })
                Assert.Null(PropertyCatalog.Find(code));
        }

        [Fact]
        public void The_description_gives_the_game_capacity()
        {
            Assert.Contains("(game: 12)", PropertyCatalog.DocksWarehouse.Describe());
            Assert.Contains("(game: 5)", PropertyCatalog.Bungalow.Describe());
            Assert.Contains("restart", PropertyCatalog.Barn.Describe());
        }
    }
}
