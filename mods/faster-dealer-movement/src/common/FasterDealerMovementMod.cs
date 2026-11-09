using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using FasterDealerMovement;

[assembly: MelonInfo(typeof(FasterDealerMovementMod), "Dealers Run This Town", "1.0.0", "DoctorShuckle")]
[assembly: MelonGame("TVGS", "Schedule I")]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif
// Patch classes are applied one by one below, so a single missing game method cannot abort the rest.
[assembly: HarmonyDontPatchAll]

// ---------------------------------------------------------------------------------------------
// How the mod works
//
// A dealer walking to a customer is driven by DealerAttendDealBehaviour. The game tells that behaviour
// when the trip starts, restarts and ends, and each dealer's NPCSpeedController keeps a stack of named
// speeds where only the highest priority counts. The mod adds one entry ("drt_deal_run", priority 11)
// when a trip starts or restarts and removes it when it is paused or ends, so a dealer runs to deals and
// walks at the game's own pace everywhere else. Priority 11 beats the game's rain and urgent-contract
// speeds (so a deal is always at the same speed) and still loses to cowering, flinching and standing up
// after a fall. The game applies the stack to the navigation agent itself, so nothing is polled and
// nothing is saved.
// ---------------------------------------------------------------------------------------------

namespace FasterDealerMovement
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class FasterDealerMovementMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            PatchLoader.ApplyAll(this);
        }

        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();
    }
}
