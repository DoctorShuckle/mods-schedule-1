using System;

namespace ModShared
{
    /// <summary>
    /// The checks every mod makes on a number read from its settings. Game-free (no game, Unity or
    /// MelonLoader types), so the unit-test projects compile it too. A value that is not usable
    /// falls back to the caller's value, which is how settings "leave the game's value alone".
    /// </summary>
    internal static class Numbers
    {
        /// <summary>True when the value is a real number: not NaN and not infinite.</summary>
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>The value when it is finite and above zero, else <paramref name="fallback"/>.</summary>
        public static float PositiveOr(float value, float fallback) => IsFinite(value) && value > 0f ? value : fallback;

        /// <summary>The value when it is finite and zero or above, else <paramref name="fallback"/>.</summary>
        public static float NonNegativeOr(float value, float fallback) => IsFinite(value) && value >= 0f ? value : fallback;

        /// <summary>The value capped at <paramref name="max"/> when it is finite and above zero, else <paramref name="fallback"/>.</summary>
        public static float PositiveCappedOr(float value, float max, float fallback)
            => IsFinite(value) && value > 0f ? Math.Min(value, max) : fallback;

        /// <summary>The value capped at <paramref name="max"/> when it is finite and zero or above, else <paramref name="fallback"/>.</summary>
        public static float NonNegativeCappedOr(float value, float max, float fallback)
            => IsFinite(value) && value >= 0f ? Math.Min(value, max) : fallback;

        /// <summary>The value pulled into min..max when it is finite, else <paramref name="fallback"/>.</summary>
        public static float ClampOr(float value, float min, float max, float fallback)
            => IsFinite(value) ? Math.Max(min, Math.Min(value, max)) : fallback;

        /// <summary>The value pulled into min..max.</summary>
        public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(value, max));
    }
}
