using System;

namespace ConfigurableEmployees
{
    /// <summary>
    /// How a configured number is turned into the value the mod writes. This and everything else in
    /// the Rules folder must not reference any game, Unity or MelonLoader type: the folder is compiled
    /// on its own into the unit-test project, so the rules can be tested without the game. The patches
    /// read the game, call these, then act.
    ///
    /// Every setting has the same escape hatch: a value that is not usable (negative, zero where
    /// zero makes no sense, NaN) means "leave the game's own value alone". The game's values for
    /// wages, signing fees and walk speed live in prefab data that nothing in the code reveals, so
    /// the mod never guesses them: it only overrides what the player explicitly set.
    /// </summary>
    internal static class Setting
    {
        /// <summary>The default for every numeric setting: do not touch the game's value.</summary>
        public const float Unset = -1f;

        public const float MaxWalkSpeed = 20f;
        public const float MaxMultiplier = 100f;
        public const int MaxAssigned = 100;

        /// <summary>Money (wage, signing fee): zero is a valid price, a negative number is "unset".</summary>
        public static float ResolveMoney(float configured, float game)
            => configured >= 0f && !float.IsInfinity(configured) ? configured : game;

        /// <summary>Walk speed in metres per second: positive and capped, else "unset".</summary>
        public static float ResolveSpeed(float configured, float game)
            => configured > 0f && !float.IsInfinity(configured) ? Math.Min(configured, MaxWalkSpeed) : game;

        /// <summary>A work-speed multiplier: positive and capped, else "unset".</summary>
        public static float ResolveMultiplier(float configured, float game)
            => configured > 0f && !float.IsInfinity(configured) ? Math.Min(configured, MaxMultiplier) : game;

        /// <summary>A whole number of things (stations, bins, routes): at least one, else "unset".</summary>
        public static int ResolveCount(int configured, int game, int max)
            => configured >= 1 ? Math.Min(configured, max) : game;
    }
}
