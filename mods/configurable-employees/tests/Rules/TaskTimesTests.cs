using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class TaskTimesTests
    {
        [Fact]
        public void Every_setting_is_unique_within_its_workers_section()
        {
            var seen = new HashSet<string>();
            foreach (TaskTime time in TaskTimes.All)
                Assert.True(seen.Add(time.Kind + "." + time.Key), time.Kind + "." + time.Key + " is listed twice");
        }

        [Fact]
        public void Every_entry_is_well_formed()
        {
            foreach (TaskTime time in TaskTimes.All)
            {
                Assert.True(time.GameSeconds > 0f, time.Key + " needs a game time");
                Assert.Contains("Seconds", time.Key);
                Assert.NotEqual(EmployeeKind.Unknown, time.Kind);
                Assert.False(string.IsNullOrWhiteSpace(time.Name));
            }
        }

        [Fact]
        public void Entries_start_unset_and_change_nothing()
        {
            foreach (TaskTime time in TaskTimes.All)
            {
                Assert.False(time.IsSet);
                Assert.Equal(1f, time.Factor);
            }
        }

        [Fact]
        public void The_factor_turns_the_game_time_into_the_configured_time()
        {
            var time = new TaskTime(EmployeeKind.Chemist, "TestSeconds", "Test", "Test", 15f, "A test");
            time.Configured = 5f;
            Assert.True(time.IsSet);
            Assert.Equal(3f, time.Factor);       // 15 s at three times the speed is 5 s
            time.Configured = 30f;
            Assert.Equal(0.5f, time.Factor);     // slower than the game
        }

        [Fact]
        public void Each_worker_that_moves_items_has_its_own_entry()
        {
            var entries = new[] { TaskTimes.MoveItemFor(EmployeeKind.Botanist), TaskTimes.MoveItemFor(EmployeeKind.Chemist), TaskTimes.MoveItemFor(EmployeeKind.Packager) };
            Assert.All(entries, e => Assert.NotNull(e));
            Assert.Equal(3, new HashSet<TaskTime>(entries).Count);
            Assert.Null(TaskTimes.MoveItemFor(EmployeeKind.Cleaner));
        }

        [Fact]
        public void The_description_gives_the_game_value()
        {
            Assert.Contains("(game: 0.5 s)", TaskTimes.PackagerMoveItem.Describe());
            Assert.Contains("(game: 20 s)", TaskTimes.ChemistryStation.Describe());
        }
    }
}
