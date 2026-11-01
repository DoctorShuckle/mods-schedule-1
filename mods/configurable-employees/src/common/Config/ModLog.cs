using MelonLoader;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Thin wrapper over the MelonLoader logger so patches can log without holding a reference to
    /// the mod instance. Extra output is gated by the Verbosity preference, the switch to turn up
    /// when reporting a problem:
    ///   0  only the startup summary, warnings and errors
    ///   1  <see cref="Verbose"/>: every change the mod makes to an employee
    ///   2  <see cref="Trace"/>: also each botanist task start and stop (a lot of lines)
    /// </summary>
    internal static class ModLog
    {
        private static MelonLogger.Instance _logger;

        /// <summary>Set from <see cref="ModConfig.Apply"/>; 0 until preferences are read.</summary>
        public static int Level { get; set; }

        public static void Init(MelonLogger.Instance logger) => _logger = logger;

        public static void Info(string message) => _logger?.Msg(message);

        public static void Warning(string message) => _logger?.Warning(message);

        public static void Verbose(string message)
        {
            if (Level >= 1) _logger?.Msg("[verbose] " + message);
        }

        public static void Trace(string message)
        {
            if (Level >= 2) _logger?.Msg("[trace] " + message);
        }
    }
}
