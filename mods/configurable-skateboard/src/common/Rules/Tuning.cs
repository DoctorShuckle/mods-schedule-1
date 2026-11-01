using System;

namespace ConfigurableSkateboard
{
    /// <summary>
    /// The mod's decisions as plain functions of plain values. Like the other Rules files it
    /// references no game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class Tuning
    {
        /// <summary>The largest multiplier accepted; a typo like 1000 is capped, not obeyed.</summary>
        public const float MaxMultiplier = 10f;

        /// <summary>
        /// A multiplier for a game value (speed, force). It must be above zero: zero would stop the
        /// board or erase the jump. Anything unusable falls back to 1, which leaves the game as it is.
        /// </summary>
        public static float Multiplier(float configured)
        {
            if (float.IsNaN(configured) || float.IsInfinity(configured) || configured <= 0f) return 1f;
            return Math.Min(configured, MaxMultiplier);
        }

        /// <summary>A multiplier for a cost. Zero is allowed (free); anything unusable falls back to 1.</summary>
        public static float CostMultiplier(float configured)
        {
            if (float.IsNaN(configured) || float.IsInfinity(configured) || configured < 0f) return 1f;
            return Math.Min(configured, MaxMultiplier);
        }

        /// <summary>
        /// A stamina change while riding: only spending (a negative change) is scaled, so regaining
        /// stamina is never touched. Off the board the change passes through unchanged.
        /// </summary>
        public static float StaminaChange(float change, float costMultiplier, bool riding)
            => riding && change < 0f ? change * costMultiplier : change;
    }
}
