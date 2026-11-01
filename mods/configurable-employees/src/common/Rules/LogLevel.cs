using System;

namespace ConfigurableEmployees
{
    /// <summary>The Verbosity setting: 0 quiet, 1 every change the mod makes, 2 also the start and stop of every timed task.</summary>
    internal static class LogLevel
    {
        /// <summary>Highest logging level.</summary>
        public const int Max = 2;

        /// <summary>The logging level to use: anything out of range is pulled back to 0..<see cref="Max"/>.</summary>
        public static int Clamp(int configured) => Math.Max(0, Math.Min(configured, Max));
    }
}
