using System;

namespace ConfigurableEmployees
{
    /// <summary>The arithmetic of changing an action's time. The table of actions is in TaskTimes.cs.</summary>
    internal static class TaskTiming
    {
        public const float MinSeconds = 0.1f;
        public const float MaxSeconds = 3600f;

        /// <summary>
        /// A task duration, scaled from what the game returned. <paramref name="gameBase"/> is the
        /// game's own number for one unit of the task (10 s to pour soil, 1 s per plant to
        /// harvest). Scaling the game's result, instead of replacing it, keeps per-plant harvest
        /// times per-plant, and keeps working if the game ever changes its numbers.
        /// </summary>
        public static float ScaleDuration(float gameResult, float gameBase, float configuredSeconds)
        {
            if (!(configuredSeconds > 0f) || !(gameBase > 0f) || float.IsInfinity(configuredSeconds)) return gameResult;
            float seconds = Math.Max(MinSeconds, Math.Min(configuredSeconds, MaxSeconds));
            return gameResult * seconds / gameBase;
        }

        /// <summary>
        /// The work speed multiplier that makes a task which takes <paramref name="gameSeconds"/> in
        /// the game take <paramref name="configuredSeconds"/> instead (1 = no change). Same limits
        /// as <see cref="ScaleDuration"/>.
        /// </summary>
        public static float TaskSpeedFactor(float gameSeconds, float configuredSeconds)
        {
            if (!(gameSeconds > 0f)) return 1f;
            float scaled = ScaleDuration(gameSeconds, gameSeconds, configuredSeconds);
            return scaled > 0f ? gameSeconds / scaled : 1f;
        }
    }
}
