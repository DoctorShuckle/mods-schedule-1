using System;
using HarmonyLib;

namespace WarehouseAccess
{
    // The game keeps the warehouse shut with three things, and the mod answers each:
    //   * DarkMarket.IsOpen, set every frame from ShouldBeOpen (hours, and no police chase). The
    //     roller doors and Igor's knock dialogue read it, so this one patch covers them.
    //   * The access zone's own GetIsOpen, which adds the opening hours and the unlocked check.
    //   * The knock: DarkMarketMainDoor offers "Knock" while KnockingEnabled. The game turns it off
    //     itself with SetKnockingEnabled(false) when the warehouse unlocks, so the mod calls that.
    // Igor stands at the door until the warehouse unlocks, so he is sent to his office.
    // DarkMarket.Unlocked is left alone: setting it also saves "WarehouseUnlocked" and runs the
    // game's unlock events, and the mod promises to change nothing that is saved.

    /// <summary>Makes the warehouse count as open at every hour, even during a police chase.</summary>
    [HarmonyPatch(typeof(DarkMarket), "ShouldBeOpen")]
    internal static class DarkMarket_ShouldBeOpen_Patch
    {
        private static void Postfix(ref bool __result)
        {
            try
            {
                if (!__result && WarehouseGate.ShouldOpen()) __result = true;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not open the warehouse: " + ex);
            }
        }
    }

    /// <summary>Lets the doors' access zone open at every hour, and before the game has unlocked it.</summary>
    [HarmonyPatch(typeof(DarkMarketAccessZone), "GetIsOpen")]
    internal static class DarkMarketAccessZone_GetIsOpen_Patch
    {
        private static void Postfix(ref bool __result)
        {
            try
            {
                if (!__result && WarehouseGate.ShouldOpen()) __result = true;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not open the warehouse doors: " + ex);
            }
        }
    }

    /// <summary>Removes the knock prompt the moment the player looks at the door.</summary>
    [HarmonyPatch(typeof(DarkMarketMainDoor), "Hovered")]
    internal static class DarkMarketMainDoor_Hovered_Patch
    {
        private static bool Prefix(DarkMarketMainDoor __instance)
        {
            try
            {
                if (!WarehouseGate.ShouldOpen()) return true;

                __instance.SetKnockingEnabled(false);
                ModLog.Verbose("Removed the knock prompt.");
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not remove the knock prompt: " + ex);
                return true;
            }
        }
    }

    /// <summary>Takes Igor off the door when he starts, if the warehouse is already open to the player.</summary>
    [HarmonyPatch(typeof(Igor), "Start")]
    internal static class Igor_Start_Patch
    {
        private static void Postfix(Igor __instance)
        {
            try
            {
                if (!WarehouseGate.ShouldOpen()) return;

                GameAccess.GuardOffice(__instance);
                ModLog.Verbose("Sent Igor to his office.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not move Igor: " + ex);
            }
        }
    }
}
