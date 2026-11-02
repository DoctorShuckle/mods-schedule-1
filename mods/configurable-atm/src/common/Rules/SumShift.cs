using System;

namespace ConfigurableAtm
{
    /// <summary>
    /// Moves the weekly deposit total down while the ATM screen's methods run, so the game's own
    /// checks against 10000 come out as they would against the real limit (see
    /// <see cref="DepositRules.ShiftedSum"/>), and puts the real total back when they end.
    /// The total lives in the game, so it is read and written through the two delegates; that keeps
    /// this class free of game types and unit-tested. Calls nest: only the outermost
    /// <see cref="Enter"/> moves the total and only the matching <see cref="Exit"/> restores it.
    /// </summary>
    internal sealed class SumShift
    {
        private readonly Func<float> _read;
        private readonly Action<float> _write;
        private int _depth;
        private bool _shifted;
        private float _real;

        public SumShift(Func<float> read, Action<float> write)
        {
            _read = read;
            _write = write;
        }

        /// <summary>The real weekly total, also while it is moved.</summary>
        public float RealSum => _shifted ? _real : _read();

        public void Enter(float limit)
        {
            if (_depth++ > 0) return; // called from another shifted method: already moved
            if (limit == DepositRules.GameLimit) return; // nothing to move

            _real = _read();
            _shifted = true;
            _write(DepositRules.ShiftedSum(_real, limit));
        }

        public void Exit()
        {
            if (_depth > 0 && --_depth > 0) return;
            _depth = 0;

            if (!_shifted) return;
            _shifted = false;
            _write(_real); // put back as it was, not by undoing the arithmetic
        }
    }
}
