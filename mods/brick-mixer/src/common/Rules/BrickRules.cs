using ModShared;
namespace BrickMixer
{
    /// <summary>The decisions of the Brick Mixer, with no game types so they can be unit-tested.</summary>
    public static class BrickRules
    {
        /// <summary>Units in a brick. The Brick Mixer mixes one brick's worth at a time: 20 units and 20 mixers.</summary>
        public const int BrickUnits = 20;

        /// <summary>True for a package of exactly one brick's units.</summary>
        public static bool IsBrick(int packageUnits) => packageUnits == BrickUnits;

        /// <summary>Bricks a finished batch of <c>units</c> becomes; 0 when it is not a whole number of bricks.</summary>
        public static int RepackBricks(int units) => units > 0 && units % BrickUnits == 0 ? units / BrickUnits : 0;

        /// <summary>Price of the Brick Mixer: the Mk2's price times the setting, never negative, in whole cents.</summary>
        public static float Price(float mk2Price, float multiplier)
        {
            if (float.IsNaN(mk2Price) || mk2Price < 0f) mk2Price = 0f;
            float price = mk2Price * multiplier;
            if (float.IsNaN(price) || price < 0f) return 0f;
            return (float)System.Math.Round(price, 2);
        }

        /// <summary>A multiplier that is a finite number above 0 and at most 1000; otherwise the fallback.</summary>
        public static float ValidMultiplier(float value, float fallback)
            => value <= 1000f ? Numbers.PositiveOr(value, fallback) : fallback;
    }
}
