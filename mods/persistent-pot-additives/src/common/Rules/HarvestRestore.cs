using System;
using System.Collections.Generic;

namespace PersistentPotAdditives
{
    /// <summary>
    /// The harvest-restore decisions as plain functions of plain values. This file must not reference any
    /// game, Unity or MelonLoader type: it is compiled on its own into the unit-test project, so
    /// the rules can be tested without the game. The patches read the game, call these, then act.
    /// </summary>
    internal static class HarvestRestore
    {
        /// <summary>
        /// Before a harvest: should we remember the pot's additives? Only the host decides, and
        /// only when the game's handler will really run (it does nothing without a plant).
        /// </summary>
        public static bool ShouldCapture(bool isServer, bool hasPlant) => isServer && hasPlant;

        /// <summary>
        /// After a harvest: is there anything to put back? <paramref name="usesLeft"/> is the
        /// count after the game used one, so it is "uses left for the NEXT plant". At zero the
        /// game has cleared the soil itself, and additives are meant to end there.
        /// </summary>
        public static bool ShouldRestore(int capturedCount, int usesLeft) => capturedCount > 0 && usesLeft > 0;

        /// <summary>
        /// The ids to re-apply, in order. Lazy on purpose: <paramref name="isApplied"/> is asked
        /// just as each id is reached, so it sees the effect of earlier applies. Skips ids that
        /// are already on the pot and repeated ids, so nothing is ever applied twice.
        /// </summary>
        public static IEnumerable<string> IdsToRestore(IEnumerable<string> captured, Func<string, bool> isApplied)
        {
            var seen = new HashSet<string>();
            foreach (string id in captured)
            {
                if (!seen.Add(id)) continue;
                if (isApplied(id)) continue;
                yield return id;
            }
        }
    }
}
