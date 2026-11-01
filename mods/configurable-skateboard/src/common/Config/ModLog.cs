using MelonLoader;

namespace ConfigurableSkateboard
{
    /// <summary>
    /// Thin wrapper over the MelonLoader logger so patches can log without holding a reference to
    /// the mod instance. <see cref="Verbose"/> is gated by the VerboseLogging preference, which is
    /// the switch to flip when reporting a problem: it prints every decision the patches make.
    /// </summary>
    internal static class ModLog
    {
        private static MelonLogger.Instance _logger;

        /// <summary>Set from <see cref="ModConfig.Apply"/>; false until preferences are read.</summary>
        public static bool VerboseEnabled { get; set; }

        public static void Init(MelonLogger.Instance logger) => _logger = logger;

        public static void Info(string message) => _logger?.Msg(message);

        public static void Warning(string message) => _logger?.Warning(message);

        public static void Verbose(string message)
        {
            if (VerboseEnabled) _logger?.Msg("[verbose] " + message);
        }
    }
}
