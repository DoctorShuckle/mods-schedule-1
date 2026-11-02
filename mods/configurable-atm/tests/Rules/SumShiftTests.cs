using Xunit;

namespace ConfigurableAtm.Tests
{
    public class SumShiftTests
    {
        /// <summary>A stand-in for the game's static total.</summary>
        private sealed class Total
        {
            public float Value;
            public readonly SumShift Shift;

            public Total(float value)
            {
                Value = value;
                Shift = new SumShift(() => Value, v => Value = v);
            }
        }

        [Fact]
        public void Moves_the_total_while_inside_and_restores_it_exactly()
        {
            var total = new Total(1234.56f);

            total.Shift.Enter(250000f);
            Assert.Equal(DepositRules.ShiftedSum(1234.56f, 250000f), total.Value);

            total.Shift.Exit();
            Assert.Equal(1234.56f, total.Value); // bit for bit: not undone by arithmetic
        }

        [Fact]
        public void Restores_exactly_for_awkward_values_and_the_unlimited_limit()
        {
            foreach (float limit in new[] { 250000f, 33333.33f, float.MaxValue })
            foreach (float real in new[] { 0f, 0.1f, 9999.99f, 123456.789f })
            {
                var total = new Total(real);
                total.Shift.Enter(limit);
                total.Shift.Exit();
                Assert.Equal(real, total.Value);
            }
        }

        [Fact]
        public void Leaves_the_total_alone_at_the_games_own_limit()
        {
            var total = new Total(4200f);

            total.Shift.Enter(DepositRules.GameLimit);
            Assert.Equal(4200f, total.Value);

            total.Shift.Exit();
            Assert.Equal(4200f, total.Value);
        }

        [Fact]
        public void Real_sum_is_the_real_total_inside_and_outside()
        {
            var total = new Total(777f);
            Assert.Equal(777f, total.Shift.RealSum);

            total.Shift.Enter(50000f);
            Assert.NotEqual(777f, total.Value);
            Assert.Equal(777f, total.Shift.RealSum);

            total.Shift.Exit();
            Assert.Equal(777f, total.Shift.RealSum);
        }

        [Fact]
        public void Nested_calls_move_once_and_restore_at_the_outermost_exit()
        {
            var total = new Total(5000f);

            total.Shift.Enter(100000f);
            float moved = total.Value;
            total.Shift.Enter(100000f);          // a shifted method called from another
            Assert.Equal(moved, total.Value);    // not moved twice

            total.Shift.Exit();                  // the inner one ends: still moved
            Assert.Equal(moved, total.Value);
            Assert.Equal(5000f, total.Shift.RealSum);

            total.Shift.Exit();
            Assert.Equal(5000f, total.Value);
        }

        [Fact]
        public void An_exit_without_an_enter_does_nothing()
        {
            var total = new Total(5000f);

            total.Shift.Exit();
            Assert.Equal(5000f, total.Value);
        }

        [Fact]
        public void A_stray_exit_after_a_balanced_pair_does_not_restore_a_stale_total()
        {
            var total = new Total(5000f);
            total.Shift.Enter(100000f);
            total.Shift.Exit();

            total.Value = 6000f;                 // the game deposits
            total.Shift.Exit();                  // e.g. a finalizer whose prefix was skipped

            Assert.Equal(6000f, total.Value);
        }

        [Fact]
        public void Works_again_after_a_full_cycle()
        {
            var total = new Total(100f);
            total.Shift.Enter(100000f);
            total.Shift.Exit();

            total.Value = 300f;
            total.Shift.Enter(100000f);
            Assert.Equal(DepositRules.ShiftedSum(300f, 100000f), total.Value);
            total.Shift.Exit();
            Assert.Equal(300f, total.Value);
        }
    }
}
