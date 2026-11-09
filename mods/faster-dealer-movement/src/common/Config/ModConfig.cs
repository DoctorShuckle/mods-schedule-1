using System.Collections.Generic;
using MelonLoader;

namespace FasterDealerMovement
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [DealersRunThisTown].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<float> _dealSpeed;
        private static MelonPreferences_Entry<bool> _includeCartelDealers;
        private static MelonPreferences_Entry<bool> _verbose;

        // Each problem is reported once, however often the preferences are re-read.
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static float DealSpeedMs { get; private set; } = DealSpeed.Default;
        public static bool IncludeCartelDealers { get; private set; }

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("DealersRunThisTown");

            _dealSpeed = category.CreateEntry("DealSpeed", DealSpeed.Default, "Deal speed (m/s)",
                "How fast a dealer moves while on the way to a deal, in metres per second. The game's walk is 1.8, "
                + "its urgent-deal speed 4.9 and the player's walk 3.25. Values from " + DealSpeed.Min + " to " + DealSpeed.Max + " are used.");
            _includeCartelDealers = category.CreateEntry("IncludeCartelDealers", false, "Include cartel dealers",
                "false changes only the dealers you recruit. true changes the cartel's dealers as well.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each time a dealer starts or stops running to a deal. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_dealSpeed == null) return;

            DealSpeedMs = DealSpeed.Resolve(_dealSpeed.Value, out bool valid);
            if (!valid) WarnOnce("DealSpeed " + _dealSpeed.Value + " is not usable; using " + DealSpeed.Default + ".");
            else if (DealSpeedMs != _dealSpeed.Value) WarnOnce("DealSpeed " + _dealSpeed.Value + " is outside " + DealSpeed.Min + " to " + DealSpeed.Max + "; using " + DealSpeedMs + ".");

            IncludeCartelDealers = _includeCartelDealers.Value;
            ModLog.VerboseEnabled = _verbose.Value;
        }

        private static void WarnOnce(string message)
        {
            if (Warned.Add(message)) ModLog.Warning(message);
        }
    }
}
