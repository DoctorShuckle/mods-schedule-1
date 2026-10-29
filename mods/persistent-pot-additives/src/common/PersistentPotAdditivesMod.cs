using System;
using HarmonyLib;
using MelonLoader;
using PersistentPotAdditives;

[assembly: MelonInfo(typeof(PersistentPotAdditivesMod), "Persistent Pot Additives", "3.0.0", "DoctorShuckle")]
[assembly: MelonGame("TVGS", "Schedule I")]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif
// Patch classes are applied one by one below, so a single missing game method cannot abort the rest.
[assembly: HarmonyDontPatchAll]

namespace PersistentPotAdditives
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own and writes no files: see the overview at the
    /// top of Patches.cs.
    /// </summary>
    public sealed class PersistentPotAdditivesMod : MelonMod
    {
        private static readonly Type[] PatchTypes =
        {
            typeof(Pot_OnPlantFullyHarvested_Patch),
            typeof(Pot_PlantSeed_Patch),
        };

        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            int applied = 0;
            foreach (Type patch in PatchTypes)
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(patch).Patch();
                    applied++;
                }
                catch (Exception ex)
                {
                    // Surface this loudly: a failed patch means that half of the feature is off.
                    LoggerInstance.Error("Patch " + patch.Name + " failed: " + ex.Message);
                }
            }

            LoggerInstance.Msg("Persistent Pot Additives 3.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + PatchTypes.Length + " patches applied.");
        }

        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();
    }
}
