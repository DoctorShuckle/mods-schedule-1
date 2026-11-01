using System;
using HarmonyLib;

namespace ConfigurableEmployees
{
    /// <summary>The Fixer's extra hiring fee, with the game's two fixed tiers made configurable.</summary>
    [HarmonyPatch(typeof(Fixer), "GetAdditionalSigningFee")]
    internal static class Fixer_GetAdditionalSigningFee_Patch
    {
        private static void Postfix(ref float __result)
        {
            try
            {
                if (FixerFee.IsVanillaFee(ModConfig.FixerEarlyFee, ModConfig.FixerEarlyCount, ModConfig.FixerLaterFee, ModConfig.FixerFeeCap)) return;

                int hires = GameAccess.LifetimeEmployeesRecruited();
                __result = FixerFee.AdditionalSigningFee(hires, ModConfig.FixerEarlyFee, ModConfig.FixerEarlyCount, ModConfig.FixerLaterFee, ModConfig.FixerFeeCap);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not compute the Fixer's fee: " + ex);
            }
        }
    }
}
