using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConfigurableAtm
{
    /// <summary>
    /// The mod's decisions as plain functions of plain values. Like the other Rules files it
    /// references no game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class DepositRules
    {
        /// <summary>The game's weekly limit, and the value the compiler inlined all over its code.</summary>
        public const float GameLimit = 10000f;

        /// <summary>The largest limit accepted; anything above is capped. Plenty to mean "no limit" too.</summary>
        public const float MaxLimit = 1000000000f;

        /// <summary>The number of amount buttons on the ATM's amount screen.</summary>
        public const int PresetCount = 6;

        /// <summary>The largest amount a single button may hold.</summary>
        public const int MaxPreset = 1000000000;

        public static readonly int[] DefaultPresets = { 20, 50, 100, 500, 1000, 5000 };

        /// <summary>
        /// The weekly limit from the setting. It must be above zero: a limit of zero would lock the
        /// ATM, which is not a setting. Anything unusable falls back to the game's own limit.
        /// </summary>
        public static float Limit(float configured)
        {
            if (float.IsNaN(configured) || float.IsInfinity(configured) || configured <= 0f) return GameLimit;
            return Math.Min(configured, MaxLimit);
        }

        /// <summary>
        /// The limit the ATM enforces. With the limit switched off nothing is ever too much, which
        /// is the largest float: the game's own arithmetic (limit minus total, total plus amount
        /// against the limit) then stays finite and never trips.
        /// </summary>
        public static float EffectiveLimit(bool limitEnabled, float configured)
            => limitEnabled ? Limit(configured) : float.MaxValue;

        /// <summary>
        /// The weekly total at which the game starts its Clean Cash quest. The game starts it when
        /// the total reaches its own limit; it must not become unreachable if the limit is set
        /// lower, nor wait longer if the limit is set higher, so it is the smaller of the two.
        /// </summary>
        public static float QuestThreshold(bool limitEnabled, float configured)
            => Math.Min(EffectiveLimit(limitEnabled, configured), GameLimit);

        /// <summary>
        /// The weekly total moved down by how far the limit is above the game's, so the game's own
        /// checks against its 10000 give the answers they would against the real limit: total below
        /// 10000, total plus an amount within 10000, and 10000 minus the total all hold exactly when
        /// they hold for the real total and limit. Used on IL2CPP, where the 10000 cannot be rewritten.
        /// </summary>
        public static float ShiftedSum(float weeklySum, float limit) => weeklySum - (limit - GameLimit);

        /// <summary>
        /// The amounts for the ATM's six buttons from the setting: six whole numbers above zero,
        /// separated by commas, semicolons or spaces. Anything else (a wrong count, text, zero,
        /// negative, too large) is not obeyed at all, so a typo cannot leave the ATM with a
        /// half-edited row: the caller then uses the game's own amounts.
        /// </summary>
        public static bool TryParsePresets(string configured, out int[] amounts)
        {
            amounts = null;
            if (string.IsNullOrWhiteSpace(configured)) return false;

            string[] parts = configured.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != PresetCount) return false;

            var parsed = new List<int>(PresetCount);
            foreach (string part in parts)
            {
                if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int value)) return false;
                if (value < 1 || value > MaxPreset) return false;
                parsed.Add(value);
            }

            amounts = parsed.ToArray();
            return true;
        }
    }
}
