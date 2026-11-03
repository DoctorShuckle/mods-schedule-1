namespace TeleportToProperties
{
    /// <summary>
    /// The one place a teleport is requested, so the hotkeys and the phone app behave alike: move the
    /// player, remember the place as the pick, and say what happened.
    /// </summary>
    internal static class Travel
    {
        /// <summary>The code of the place picked last, by the keys or the phone. Kept by code so a changing list does not move it.</summary>
        public static string PickedCode;

        public static bool Go(Place place)
        {
            string refusal = GameAccess.TeleportTo(place, ModConfig.UseInterior, out Vector3 _);
            if (refusal.Length > 0)
            {
                Toast.Show(refusal);
                ModLog.Verbose("Refused: " + refusal + ".");
                return false;
            }

            PickedCode = place.Code;
            Toast.Show("Teleported to " + place.Name);
            ModLog.Verbose("Teleported to " + place.Name + ".");
            return true;
        }
    }
}
