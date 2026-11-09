using ModShared;
namespace FasterDealerMovement
{
    /// <summary>
    /// The mod's decisions as plain functions of plain values. Like the other Rules files it
    /// references no game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class DealSpeed
    {
        /// <summary>Metres per second. Above the game's urgent-deal speed (4.9) and rain speed (5.4), under its 7 m/s ceiling.</summary>
        public const float Default = 6f;

        public const float Min = 1f;

        public const float Max = 12f;

        /// <summary>
        /// Priority of the mod's entry in a dealer's speed stack, which only uses the highest priority.
        /// The game's own entries: default 0, rain 2, urgent contract 10, and 80 to 100 for cowering,
        /// flinching and standing up from a ragdoll. 11 beats rain and urgent, and still loses to those last three.
        /// </summary>
        public const int Priority = 11;

        /// <summary>Id of the entry in the speed stack. Adding the same id again updates it, so it never doubles up.</summary>
        public const string EntryId = "drt_deal_run";

        /// <summary>
        /// The speed to use for a configured value: NaN, infinity and anything not above zero fall back
        /// to <see cref="Default"/>, and the rest is moved into <see cref="Min"/> to <see cref="Max"/>.
        /// <paramref name="valid"/> is false when the value was replaced because it was unusable, which is when to warn.
        /// </summary>
        public static float Resolve(float configured, out bool valid)
        {
            if (!Numbers.IsFinite(configured) || configured <= 0f)
            {
                valid = false;
                return Default;
            }

            valid = true;
            return configured < Min ? Min : configured > Max ? Max : configured;
        }
    }
}
