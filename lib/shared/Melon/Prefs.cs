using MelonLoader;

namespace ModShared
{
    /// <summary>The preference every mod has: VerboseLogging, wired to <see cref="ModLog"/>.</summary>
    internal static class Prefs
    {
        /// <summary>Creates the VerboseLogging entry (default off). The description says what the mod logs.</summary>
        public static MelonPreferences_Entry<bool> CreateVerbose(MelonPreferences_Category category, string description)
            => category.CreateEntry("VerboseLogging", false, "Verbose logging", description);

        /// <summary>Copies the entry's current value into <see cref="ModLog"/>. Call from the mod's Apply.</summary>
        public static void ApplyVerbose(MelonPreferences_Entry<bool> entry) => ModLog.VerboseEnabled = entry.Value;
    }
}
