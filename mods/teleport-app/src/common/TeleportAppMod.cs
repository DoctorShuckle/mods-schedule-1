using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using TeleportApp;

[assembly: MelonInfo(typeof(TeleportAppMod), "Teleport App", "1.0.0", "DoctorShuckle")]
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
// A "TP" icon on the phone opens the game's own list popup: first the kind of place (properties,
// businesses, dealers), then the places of that kind. Three keys do the same without the phone:
// previous place, next place, teleport. The places are the properties and businesses the player
// owns and the dealers the player has hired, read from the game's own lists each time, so a place
// bought later shows up without a restart. Teleporting calls the game's own PlayerMovement.Teleport
// on the local player only. One patch (on opening the phone) adds the icon; nothing is saved or
// sent over the network.
// ---------------------------------------------------------------------------------------------

namespace TeleportApp
{
    /// <summary>
    /// Entry point. Folders: Game (reads and calls into the game), Hud (the on-screen line), Rules
    /// (the game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class TeleportAppMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            List<Type> patches = FindPatches();
            int applied = 0;
            foreach (Type patch in patches)
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(patch).Patch();
                    applied++;
                }
                catch (Exception ex)
                {
                    // Loud on purpose: a failed patch means the TP icon is off. The keys still work.
                    LoggerInstance.Error("Patch " + patch.Name + " failed: " + ex.Message);
                }
            }

            LoggerInstance.Msg("Teleport App 1.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + patches.Count + " patches applied. Keys: " + ModConfig.PreviousKey + " / " + ModConfig.NextKey + " pick a place, " + ModConfig.TeleportKey + " teleports.");
        }

        // Per-frame work, because nothing can be hooked: a key press raises no game event. The check is
        // one cheap key test per frame, and nothing else runs here (see the README).
        public override void OnUpdate()
        {
            if (!ModConfig.EnableHotkeys || GameAccess.InputBlocked()) return;

            if (Input.GetKeyDown(ModConfig.PreviousKey)) Pick(-1);
            else if (Input.GetKeyDown(ModConfig.NextKey)) Pick(+1);
            else if (Input.GetKeyDown(ModConfig.TeleportKey)) Go();
        }

        // IMGUI is drawn by the engine every frame; Toast.Draw returns at once while no line is showing.
        public override void OnGUI() => Toast.Draw();

        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();

        /// <summary>Every type in this assembly marked [HarmonyPatch], in name order so the log reads the same each launch.</summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(TeleportAppMod).Assembly.GetTypes(); }
            // A type that cannot load (a game class that has gone) must not hide the rest.
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            var patches = new List<Type>();
            foreach (Type type in types)
                if (type != null && type.IsDefined(typeof(HarmonyPatch), false)) patches.Add(type);
            patches.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return patches;
        }

        private void Pick(int delta)
        {
            List<Place> places = GameAccess.RotationPlaces();
            if (places.Count == 0)
            {
                Toast.Show("Nothing in the hotkey rotation");
                return;
            }

            int index = Selection.Step(Selection.Resolve(Codes(places), Travel.PickedCode), places.Count, delta);
            Travel.PickedCode = places[index].Code;
            Toast.Show("Teleport to: " + places[index].Label + "  (" + (index + 1) + "/" + places.Count + ")");
            ModLog.Verbose("Picked " + places[index].Name + " [" + places[index].Code + "].");
        }

        private void Go()
        {
            List<Place> places = GameAccess.RotationPlaces();
            int index = Selection.Resolve(Codes(places), Travel.PickedCode);
            if (index < 0)
            {
                Toast.Show("Nothing in the hotkey rotation");
                return;
            }

            Travel.Go(places[index]);
        }

        private static List<string> Codes(List<Place> places)
        {
            var codes = new List<string>(places.Count);
            foreach (Place place in places) codes.Add(place.Code);
            return codes;
        }
    }
}
