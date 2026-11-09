using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using WarehouseAccess;

[assembly: MelonInfo(typeof(WarehouseAccessMod), "Warehouse Access", "1.0.0", "DoctorShuckle")]
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
// The game keeps the warehouse shut by hours, by a police chase, by a "Knock" prompt at the door and
// by Igor standing guard at it. The mod asks one question (WarehouseGate): has the warehouse quest
// been completed, and is the mod set to always open or does the player have the rank it asks for?
// When the answer is yes, four small patches (WarehousePatches) make the game's own checks agree.
// No game code is replaced, nothing is saved, and DarkMarket.Unlocked is left alone because setting
// it would also save the unlock and run the game's unlock events.
// ---------------------------------------------------------------------------------------------

namespace WarehouseAccess
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings and the quest it found: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class WarehouseAccessMod : MelonMod
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
