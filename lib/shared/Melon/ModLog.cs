using System.Collections.Generic;
using MelonLoader;

namespace ModShared
{
    /// <summary>
    /// Thin wrapper over the MelonLoader logger so patches can log without holding a reference to
    /// the mod instance. Compiled into every mod (see shared/), so each mod gets its own copy and
    /// its own state. Extra output is gated by <see cref="Level"/>, the switch to turn up when
    /// reporting a problem:
    ///   0  only the startup summary, warnings and errors
    ///   1  <see cref="Verbose"/>: every decision the patches make (the VerboseLogging preference)
    ///   2  <see cref="Trace"/>: also high-volume detail (for mods that have a Verbosity preference)
    /// </summary>
    internal static class ModLog
    {
        private static MelonLogger.Instance _logger;
        private static readonly HashSet<string> Warned = new HashSet<string>();

        /// <summary>Set from the mod's ModConfig.Apply; 0 until preferences are read.</summary>
        public static int Level { get; set; }

        /// <summary>The on/off form of <see cref="Level"/> for mods with a single VerboseLogging switch.</summary>
        public static bool VerboseEnabled
        {
            get => Level >= 1;
            set => Level = value ? 1 : 0;
        }

        public static void Init(MelonLogger.Instance logger) => _logger = logger;

        public static void Info(string message) => _logger?.Msg(message);

        public static void Warning(string message) => _logger?.Warning(message);

        /// <summary>Warns the first time a message is seen, so a repeated problem does not flood the log.</summary>
        public static void WarnOnce(string message)
        {
            if (Warned.Add(message)) _logger?.Warning(message);
        }

        private static readonly Dictionary<string, string> LastProblem = new Dictionary<string, string>();

        /// <summary>
        /// Logs a problem when it first appears or changes. Pass null when there is none: that forgets the
        /// problem, so it is said again if it returns. For settings checked on every preferences event
        /// (they arrive in bursts), where <see cref="WarnOnce"/> would never repeat a fixed-then-broken value.
        /// </summary>
        public static void WarnChanged(string key, string problem)
        {
            LastProblem.TryGetValue(key, out string last);
            if (problem == last) return;
            if (problem == null) LastProblem.Remove(key); else LastProblem[key] = problem;
            if (problem != null) _logger?.Warning(problem);
        }

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
