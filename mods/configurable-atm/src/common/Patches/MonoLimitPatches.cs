#if !IL2CPP
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ConfigurableAtm
{
    // ---------------------------------------------------------------------------------------------
    // The game's weekly limit is a const, so its compiler wrote the number 10000 straight into every
    // method that uses it: nothing reads ATM.WeeklyDepositLimit at run time, and changing that field
    // would do nothing. Each patch here is a transpiler: it rewrites one method's IL, replacing the
    // literal 10000 with a call that returns the configured value. A method with a different count
    // of literals than expected makes the patch fail loudly (the mod logs it) and leaves the method
    // as the game wrote it.
    //
    // This works on Mono, where the game's IL is what runs. On IL2CPP the IL is gone: see
    // Il2CppLimitPatches.cs.
    // ---------------------------------------------------------------------------------------------

    internal static class LimitLiteral
    {
        private static readonly MethodInfo Limit = AccessTools.Method(typeof(LimitHooks), nameof(LimitHooks.Limit));
        private static readonly MethodInfo QuestThreshold = AccessTools.Method(typeof(LimitHooks), nameof(LimitHooks.QuestThreshold));

        public static IEnumerable<CodeInstruction> ReplaceWithLimit(IEnumerable<CodeInstruction> code, int expected, string method)
            => Replace(code, Limit, expected, method);

        public static IEnumerable<CodeInstruction> ReplaceWithQuestThreshold(IEnumerable<CodeInstruction> code, int expected, string method)
            => Replace(code, QuestThreshold, expected, method);

        private static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> code, MethodInfo replacement, int expected, string method)
        {
            var result = new List<CodeInstruction>(code);
            int found = 0;
            foreach (CodeInstruction instruction in result)
            {
                if (instruction.opcode != OpCodes.Ldc_R4 || !(instruction.operand is float value) || value != DepositRules.GameLimit) continue;

                // Edited in place so any label or exception block that starts here is kept.
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                found++;
            }

            if (found != expected)
                throw new System.InvalidOperationException(method + " has " + found + " uses of the limit, expected " + expected + ": the game has changed.");

            ModLog.Verbose(method + ": replaced " + found + " use(s) of " + DepositRules.GameLimit + " with " + replacement.Name + ".");
            return result;
        }
    }

    /// <summary>The "remaining allowed deposit" the amount clamps and the MAX button use.</summary>
    [HarmonyPatch(typeof(ATMInterface), "get_remainingAllowedDeposit")]
    internal static class ATMInterface_remainingAllowedDeposit_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
            => LimitLiteral.ReplaceWithLimit(code, 1, "ATMInterface.remainingAllowedDeposit");
    }

    /// <summary>The "spent / limit" readout and its red colour, and the menu's Deposit button.</summary>
    [HarmonyPatch(typeof(ATMInterface), "Update")]
    internal static class ATMInterface_Update_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
            => LimitLiteral.ReplaceWithLimit(code, 3, "ATMInterface.Update");
    }

    /// <summary>Which amount buttons are greyed out for going over the limit.</summary>
    [HarmonyPatch(typeof(ATMInterface), "UpdateAvailableAmounts")]
    internal static class ATMInterface_UpdateAvailableAmounts_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
            => LimitLiteral.ReplaceWithLimit(code, 1, "ATMInterface.UpdateAvailableAmounts");
    }

    /// <summary>When the Clean Cash quest starts.</summary>
    [HarmonyPatch(typeof(Quest_CleanCash), "OnUncappedMinPass")]
    internal static class Quest_CleanCash_OnUncappedMinPass_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
            => LimitLiteral.ReplaceWithQuestThreshold(code, 1, "Quest_CleanCash.OnUncappedMinPass");
    }
}
#endif
