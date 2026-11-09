using System;
using MelonLoader;
#if IL2CPP
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
#endif

namespace FasterDealerMovement
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
        /// <summary>
        /// Puts the dealer on the deal speed: one entry in its speed stack, which the game turns into the
        /// navigation agent's speed itself. Does nothing for an NPC that is not a dealer the mod covers.
        /// </summary>
        public static void StartRunning(DealerAttendDealBehaviour behaviour)
        {
            NPC npc = behaviour.Npc;
            if (npc == null || !Covers(npc)) return;

            npc.Movement.SpeedController.AddSpeedControl(
                DealSpeed.EntryId, ModConfig.DealSpeedMs, DealSpeed.Priority, SpeedControl.EType.Absolute);
            ModLog.Verbose(npc.FullName + " runs to a deal at " + ModConfig.DealSpeedMs + " m/s.");
        }

        /// <summary>
        /// Takes the entry off again. Not gated by <see cref="Covers"/> or the settings: removing an id that is
        /// not there does nothing, and an entry added earlier must never be left behind.
        /// </summary>
        public static void StopRunning(DealerAttendDealBehaviour behaviour)
        {
            NPC npc = behaviour.Npc;
            if (npc == null) return;

            npc.Movement.SpeedController.RemoveSpeedControl(DealSpeed.EntryId);
            ModLog.Verbose(npc.FullName + " stops running.");
        }

        // Recruited dealers always; cartel dealers only when the setting says so.
        private static bool Covers(NPC npc)
            => ModConfig.IncludeCartelDealers || As<CartelDealer>(npc) == null;
    }
}
