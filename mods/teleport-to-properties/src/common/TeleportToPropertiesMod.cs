using System.Collections.Generic;
using MelonLoader;
using TeleportToProperties;

[assembly: MelonInfo(typeof(TeleportToPropertiesMod), "Teleport to Properties", "1.0.0", "DoctorShuckle")]
[assembly: MelonGame("TVGS", "Schedule I")]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif

// ---------------------------------------------------------------------------------------------
// How the mod works
//
// Three keys: previous place, next place, teleport. The places are the properties and businesses the
// player owns, read from the game's own lists each time a key is pressed, so a place bought later
// shows up without a restart. Teleporting calls the game's own PlayerMovement.Teleport on the local
// player only. Nothing is patched, saved or sent over the network.
// ---------------------------------------------------------------------------------------------

namespace TeleportToProperties
{
    /// <summary>
    /// Entry point. Folders: Game (reads and calls into the game), Hud (the on-screen line), Rules
    /// (the game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class TeleportToPropertiesMod : MelonMod
    {
        // Remembered by code, so the pick survives the list changing (see Selection).
        private string _pickedCode;

        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            LoggerInstance.Msg("Teleport to Properties 1.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + ModConfig.PreviousKey + " / " + ModConfig.NextKey + " pick a place, " + ModConfig.TeleportKey + " teleports.");
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

        private void Pick(int delta)
        {
            List<Place> places = GameAccess.OwnedPlaces();
            if (places.Count == 0)
            {
                Toast.Show("You own no properties yet");
                return;
            }

            int index = Selection.Step(Selection.Resolve(Codes(places), _pickedCode), places.Count, delta);
            _pickedCode = places[index].Code;
            Toast.Show("Teleport to: " + places[index].Name + "  (" + (index + 1) + "/" + places.Count + ")");
            ModLog.Verbose("Picked " + places[index].Name + " [" + _pickedCode + "].");
        }

        private void Go()
        {
            List<Place> places = GameAccess.OwnedPlaces();
            int index = Selection.Resolve(Codes(places), _pickedCode);
            if (index < 0)
            {
                Toast.Show("You own no properties yet");
                return;
            }

            Place place = places[index];
            string refusal = GameAccess.TeleportTo(place, ModConfig.UseInterior, out Vector3 _);
            if (refusal.Length == 0)
            {
                _pickedCode = place.Code;
                Toast.Show("Teleported to " + place.Name);
                ModLog.Verbose("Teleported to " + place.Name + ".");
            }
            else
            {
                Toast.Show(refusal);
                ModLog.Verbose("Refused: " + refusal + ".");
            }
        }

        private static List<string> Codes(List<Place> places)
        {
            var codes = new List<string>(places.Count);
            foreach (Place place in places) codes.Add(place.Code);
            return codes;
        }
    }
}
