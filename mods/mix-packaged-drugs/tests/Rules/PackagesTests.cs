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

    public class PackagesThatFitTests
    {
        [Theory]
        [InlineData(20, 0, 20, 1)]    // an empty slot takes a brick
        [InlineData(20, 1, 20, 0)]    // 19 units of room: the brick is refused, not split
        [InlineData(20, 0, 5, 4)]
        [InlineData(20, 12, 5, 1)]
        [InlineData(20, 20, 1, 0)]
        [InlineData(20, 25, 1, 0)]    // an overfull slot takes nothing
        [InlineData(10, 0, 20, 0)]    // a brick never fits a limit of 10
        [InlineData(20, 0, 0, 20)]
        public void Counts_whole_packages_only(int limit, int current, int amount, int expected)
            => Assert.Equal(expected, Packages.PackagesThatFit(limit, current, amount));
    }

    public class RepackQuantityTests
    {
        [Theory]
        [InlineData(20, 20, 1)]   // a full batch from a brick is a brick
        [InlineData(10, 5, 2)]    // two jars
        [InlineData(7, 1, 7)]     // baggies always divide
        [InlineData(10, 20, 0)]   // half a brick stays loose
        [InlineData(15, 10, 0)]
        [InlineData(0, 5, 0)]
        [InlineData(10, 0, 0)]
        public void Packs_whole_packages_or_nothing(int units, int amount, int expected)
            => Assert.Equal(expected, Packages.RepackQuantity(units, amount));
    }

    public class IsAllowedTests
    {
        [Theory]
        [InlineData(1, true, false, false, true)]
        [InlineData(1, false, true, true, false)]
        [InlineData(5, false, true, false, true)]
        [InlineData(5, true, false, true, false)]
        [InlineData(20, false, false, true, true)]
        [InlineData(20, true, true, false, false)]
        public void Follows_the_switch_for_each_size(int amount, bool bags, bool jars, bool bricks, bool expected)
            => Assert.Equal(expected, Packages.IsAllowed(amount, bags, jars, bricks, allowOther: false));

        [Theory]
        [InlineData(3, true, true)]
        [InlineData(3, false, false)]
        public void Other_sizes_follow_allowOther(int amount, bool allowOther, bool expected)
            => Assert.Equal(expected, Packages.IsAllowed(amount, false, false, false, allowOther));
    }

    public class PackagingMemoryTests
    {
        [Fact]
        public void A_written_file_reads_back()
        {
            var packaging = new System.Collections.Generic.Dictionary<string, string>
            {
                ["1b4e28ba-2fa1-11d2-883f-b9a761bde3fb"] = "brick",
                ["9f0c2d4e-0000-4000-8000-000000000001"] = "jar",
            };
            var back = PackagingMemory.Parse(PackagingMemory.Serialize(packaging));
            Assert.Equal(packaging, back);
        }

        [Fact]
        public void An_empty_or_missing_text_reads_as_nothing()
        {
            Assert.Empty(PackagingMemory.Parse(null));
            Assert.Empty(PackagingMemory.Parse(""));
            Assert.Empty(PackagingMemory.Parse(PackagingMemory.Serialize(new System.Collections.Generic.Dictionary<string, string>())));
        }

        [Fact]
        public void Damaged_lines_are_skipped_and_good_ones_kept()
        {
            string text = "# header\r\nnotabs\r\n\r\na\tb\tc\r\ngood\tbrick\r\n\tempty-key\r\nkey-only\t\r\n";
            var back = PackagingMemory.Parse(text);
            Assert.Equal(new System.Collections.Generic.Dictionary<string, string> { ["good"] = "brick" }, back);
        }

        [Fact]
        public void A_value_that_would_break_the_format_is_not_written()
        {
            var packaging = new System.Collections.Generic.Dictionary<string, string> { ["a"] = "x\ty", ["b"] = "jar" };
            Assert.Equal(new System.Collections.Generic.Dictionary<string, string> { ["b"] = "jar" },
                PackagingMemory.Parse(PackagingMemory.Serialize(packaging)));
        }

        [Fact]
        public void The_last_line_wins_for_a_repeated_station()
            => Assert.Equal("jar", PackagingMemory.Parse("a\tbrick\na\tjar\n")["a"]);

        [Fact]
        public void File_names_are_stable_safe_and_differ_by_path()
        {
            string a = PackagingMemory.FileNameFor(@"C:\Users\me\AppData\LocalLow\TVGS\Schedule I\Saves\123\SaveGame_1");
            string b = PackagingMemory.FileNameFor(@"C:\Users\you\AppData\LocalLow\TVGS\Schedule I\Saves\123\SaveGame_1");
            Assert.Equal(a, PackagingMemory.FileNameFor(@"C:\Users\me\AppData\LocalLow\TVGS\Schedule I\Saves\123\SaveGame_1"));
            Assert.NotEqual(a, b);
            Assert.StartsWith("SaveGame_1-", a);
            Assert.EndsWith(".txt", a);
            Assert.DoesNotContain("/", a);
            Assert.DoesNotContain("\\", a);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("/")]
        [InlineData("a b/c:d*e")]
        public void File_names_are_valid_for_odd_paths(string path)
        {
            string name = PackagingMemory.FileNameFor(path);
            Assert.Equal(-1, name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()));
            Assert.True(name.Length > 5);
        }
    }
}
