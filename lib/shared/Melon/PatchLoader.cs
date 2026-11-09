using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace ModShared
{
    /// <summary>
    /// Applies a mod's Harmony patch classes one at a time, so a single missing game method disables
    /// one feature instead of the whole mod, then logs "&lt;Name&gt; &lt;ver&gt; (IL2CPP|Mono): N/M patches applied."
    /// </summary>
    internal static class PatchLoader
    {
        /// <summary>
        /// The patch classes: every type in the mod's assembly marked [HarmonyPatch], in name order so
        /// the log reads the same each launch. Adding a patch class is enough; the startup count in
        /// the log shows if one is missing.
        /// </summary>
        public static List<Type> FindPatches(Assembly assembly)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            // A type that cannot load (a game class that has gone) must not hide the rest.
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            var patches = new List<Type>();
            foreach (Type type in types)
                if (type != null && type.IsDefined(typeof(HarmonyPatch), false)) patches.Add(type);
            patches.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return patches;
        }

        /// <summary>
        /// Applies every patch class of the mod. <paramref name="isSkipped"/>, when given, is asked
        /// for each class name; a skipped class is warned about and left out of the count.
        /// Returns how many patches were applied.
        /// </summary>
        public static int ApplyAll(MelonMod mod, Func<string, bool> isSkipped = null)
        {
            MelonLogger.Instance log = mod.LoggerInstance;
            List<Type> patches = FindPatches(mod.GetType().Assembly);
            if (patches.Count == 0) log.Error("No patches found: the mod will do nothing.");

            int applied = 0;
            int skipped = 0;
            foreach (Type patch in patches)
            {
                if (isSkipped != null && isSkipped(patch.Name))
                {
                    log.Warning("Patch " + patch.Name + " skipped (SkipPatches).");
                    skipped++;
                    continue;
                }

                try
                {
                    mod.HarmonyInstance.CreateClassProcessor(patch).Patch();
                    applied++;
                }
                catch (Exception ex)
                {
                    // Surface this loudly: a failed patch means that feature is off.
                    log.Error("Patch " + patch.Name + " failed: " + ex.Message);
                }
            }

            log.Msg(mod.Info.Name + " " + mod.Info.Version + " (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + (patches.Count - skipped) + " patches applied" +
                (skipped > 0 ? " (" + skipped + " skipped)." : "."));
            return applied;
        }
    }
}
