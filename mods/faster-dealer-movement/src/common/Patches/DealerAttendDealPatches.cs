using System;
using HarmonyLib;

namespace FasterDealerMovement
{
    // A dealer on the way to a customer is driven by DealerAttendDealBehaviour, and the game tells that
    // behaviour when the trip starts (Activate), restarts (Resume) and ends (Pause, Deactivate). The mod
    // adds its speed entry on the first two and removes it on the last two. Nothing is polled and nothing
    // is saved: the entry lives in the dealer's speed stack for as long as the trip does.
    // Each override is declared on DealerAttendDealBehaviour itself, so Harmony patches it directly.

    /// <summary>The trip to a deal starts.</summary>
    [HarmonyPatch(typeof(DealerAttendDealBehaviour), "Activate")]
    internal static class DealerAttendDealBehaviour_Activate_Patch
    {
        private static void Postfix(DealerAttendDealBehaviour __instance)
        {
            try { GameAccess.StartRunning(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not speed up a dealer: " + ex); }
        }
    }

    /// <summary>The trip to a deal carries on after an interruption.</summary>
    [HarmonyPatch(typeof(DealerAttendDealBehaviour), "Resume")]
    internal static class DealerAttendDealBehaviour_Resume_Patch
    {
        private static void Postfix(DealerAttendDealBehaviour __instance)
        {
            try { GameAccess.StartRunning(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not speed up a dealer: " + ex); }
        }
    }

    /// <summary>The trip is interrupted (a flinch, a fight, a phone call).</summary>
    [HarmonyPatch(typeof(DealerAttendDealBehaviour), "Pause")]
    internal static class DealerAttendDealBehaviour_Pause_Patch
    {
        private static void Postfix(DealerAttendDealBehaviour __instance)
        {
            try { GameAccess.StopRunning(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not slow a dealer down: " + ex); }
        }
    }

    /// <summary>The deal is over or abandoned.</summary>
    [HarmonyPatch(typeof(DealerAttendDealBehaviour), "Deactivate")]
    internal static class DealerAttendDealBehaviour_Deactivate_Patch
    {
        private static void Postfix(DealerAttendDealBehaviour __instance)
        {
            try { GameAccess.StopRunning(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not slow a dealer down: " + ex); }
        }
    }
}
