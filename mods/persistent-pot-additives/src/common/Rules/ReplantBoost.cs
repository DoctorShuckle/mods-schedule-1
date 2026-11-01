using System.Collections.Generic;

namespace PersistentPotAdditives
{
    /// <summary>
    /// The replant growth-boost decisions as plain functions of plain values. Like the other
    /// Rules files it references no game, Unity or MelonLoader type, so it is unit-tested.
    /// </summary>
    internal static class ReplantBoost
    {
        /// <summary>
        /// After a seed is planted: should Speed Grow's growth boost be added? Fresh plantings
        /// (player and NPC) always pass a seed progress of 0; a restored save passes its saved
        /// progress, and also happens while the save is loading. Both mean "not a new planting".
        /// </summary>
        public static bool ShouldBoost(bool isServer, bool isLoadingSave, float seedProgress, bool hasPlant, float boost)
            => isServer && !isLoadingSave && seedProgress <= 0f && hasPlant && boost > 0f;

        /// <summary>Sum of the positive growth boosts; zero or negative entries are ignored.</summary>
        public static float TotalGrowth(IEnumerable<float> boosts)
        {
            float total = 0f;
            foreach (float boost in boosts)
            {
                if (boost > 0f) total += boost;
            }
            return total;
        }
    }
}
