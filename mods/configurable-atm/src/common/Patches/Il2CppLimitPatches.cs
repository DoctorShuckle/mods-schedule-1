#if IL2CPP
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppFishNet;

namespace ConfigurableAtm
{
    // ---------------------------------------------------------------------------------------------
    // On IL2CPP the game's code is native: the literal 10000 inside ATMInterface's methods cannot be
    // rewritten as the Mono build does. Overwriting what those methods decide afterwards does not work
    // either: the game's Update first sets the Deposit and amount buttons by its own 10000 and then
    // the mod sets them back, and Unity drops a button's controller selection whenever it is made
    // non-interactable, so the controller lost its place every frame (the mouse still worked).
    //
    // So the game is left to decide, and is given a total it will decide correctly on: while one of
    // the ATM screen's methods runs, ATM.WeeklyDepositSum is moved down by how far the limit is above
    // 10000 (DepositRules.ShiftedSum), so every check against 10000 comes out as it would against the
    // real limit. The real total is put back, exactly, when the method ends, even if it throws. Only
    // the "spent / limit" text, which would show the moved total, is rewritten.
    // ---------------------------------------------------------------------------------------------

    internal static class AtmTotal
    {
        /// <summary>The game's weekly total, moved while an ATM screen method runs.</summary>
        public static readonly SumShift Shift = new SumShift(() => ATM.WeeklyDepositSum, value => ATM.WeeklyDepositSum = value);
    }

    /// <summary>
    /// The ATM screen methods that check the limit, directly or by calling one that does, other than
    /// Update (which has its own patch below). Wrapping the callers as well as the methods that hold the
    /// 10000 keeps this right if IL2CPP has inlined one into another.
    /// </summary>
    [HarmonyPatch]
    internal static class ATMInterface_Limit_Shift_Patch
    {
        private static readonly string[] Methods =
        {
            "UpdateAvailableAmounts", "GetAmountFromIndex", "SetSelectedAmount",
            "SetActiveScreen", "AmountSelected", "DefaultAmountSelection", "ChangeAmount",
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string name in Methods)
                yield return AccessTools.Method(typeof(ATMInterface), name)
                    ?? throw new MissingMethodException("ATMInterface." + name);
            yield return AccessTools.PropertyGetter(typeof(ATMInterface), "remainingAllowedDeposit")
                ?? throw new MissingMethodException("ATMInterface.remainingAllowedDeposit");
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix() => AtmTotal.Shift.Enter(ModConfig.EffectiveLimit);

        // Runs after every postfix, so they still see the moved total.
        private static void Finalizer() => AtmTotal.Shift.Exit();
    }

    /// <summary>
    /// Update runs every frame on every ATM, 17 of them, and the game's own Update does nothing unless the
    /// ATM is open, so the total is moved only for the open one. Afterwards it rewrites the "spent / limit"
    /// text the game drew with the moved total and its own 10000.
    /// </summary>
    [HarmonyPatch(typeof(ATMInterface), nameof(ATMInterface.Update))]
    internal static class ATMInterface_Update_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ATMInterface __instance, out bool __state)
        {
            __state = __instance.IsOpen;
            if (__state) AtmTotal.Shift.Enter(ModConfig.EffectiveLimit);
        }

        private static void Postfix(ATMInterface __instance)
        {
            try
            {
                if (!__instance.IsOpen || !ModConfig.LimitEnabled) return;

                __instance.depositLimitText.text =
                    MoneyManager.FormatAmount(AtmTotal.Shift.RealSum) + " / " + MoneyManager.FormatAmount(ModConfig.EffectiveLimit);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not redraw the deposit limit readout: " + ex);
            }
        }

        // Runs after the postfix, which still sees the moved total.
        private static void Finalizer(bool __state)
        {
            if (__state) AtmTotal.Shift.Exit();
        }
    }

    /// <summary>
    /// Starts the Clean Cash quest at a limit lower than the game's. The game's own check, for the
    /// total reaching 10000, still runs first and still starts it at or above that.
    /// </summary>
    [HarmonyPatch(typeof(Quest_CleanCash), nameof(Quest_CleanCash.OnUncappedMinPass))]
    internal static class Quest_CleanCash_OnUncappedMinPass_Patch
    {
        private static void Postfix(Quest_CleanCash __instance)
        {
            try
            {
                if (__instance.State != EQuestState.Inactive || !InstanceFinder.IsServer) return;
                if (ATM.WeeklyDepositSum < ModConfig.QuestThreshold) return;

                ModLog.Verbose("Starting the Clean Cash quest at " + ATM.WeeklyDepositSum + ".");
                __instance.Begin(true);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not start the Clean Cash quest: " + ex);
            }
        }
    }
}
#endif
