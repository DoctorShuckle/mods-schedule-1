using System;

namespace ConfigurableEmployees
{
    /// <summary>The Fixer's extra hiring fee, with the game's two fixed tiers made configurable.</summary>
    internal static class FixerFee
    {
        // The Fixer's extra hiring fee is hard-coded in the game: 100 for each of the first six
        // hires, 250 for every later one, never more than 500 in total.
        public const float VanillaEarlyHireFee = 100f;
        public const int VanillaEarlyHireCount = 6;
        public const float VanillaLaterHireFee = 250f;
        public const float VanillaFeeCap = 500f;

        /// <summary>
        /// The Fixer's extra hiring fee for someone who has already hired <paramref name="hires"/>
        /// people over the whole save. Bad values fall back to the game's own numbers.
        /// </summary>
        public static float AdditionalSigningFee(int hires, float earlyFee, int earlyCount, float laterFee, float cap)
        {
            earlyFee = Setting.ResolveMoney(earlyFee, VanillaEarlyHireFee);
            laterFee = Setting.ResolveMoney(laterFee, VanillaLaterHireFee);
            cap = Setting.ResolveMoney(cap, VanillaFeeCap);
            if (earlyCount < 0) earlyCount = VanillaEarlyHireCount;

            float total = 0f;
            for (int i = 0; i < hires; i++) total += i < earlyCount ? earlyFee : laterFee;
            return Math.Min(total, cap);
        }

        /// <summary>True when the settings equal the game's, so the patch can leave the result alone.</summary>
        public static bool IsVanillaFee(float earlyFee, int earlyCount, float laterFee, float cap)
            => Setting.ResolveMoney(earlyFee, VanillaEarlyHireFee) == VanillaEarlyHireFee
               && (earlyCount < 0 || earlyCount == VanillaEarlyHireCount)
               && Setting.ResolveMoney(laterFee, VanillaLaterHireFee) == VanillaLaterHireFee
               && Setting.ResolveMoney(cap, VanillaFeeCap) == VanillaFeeCap;
    }
}
