namespace MixPackagedDrugs
{
    /// <summary>
    /// The arithmetic of packaged product. A package holds <c>amount</c> units (bag 1, jar 5, brick 20),
    /// and the mixing station works in units: one mixer for each.
    /// </summary>
    public static class Packages
    {
        /// <summary>Units in a stack of packages. A zero or negative amount counts as 1 (unpackaged).</summary>
        public static int Units(int quantity, int amount)
        {
            if (quantity <= 0) return 0;
            return quantity * (amount < 1 ? 1 : amount);
        }

        /// <summary>
        /// Whole packages that fit in <c>room</c> units of space. A package is never split, so a brick that
        /// does not fully fit is refused.
        /// </summary>
        public static int PackagesForRoom(int room, int amount)
        {
            if (room <= 0) return 0;
            return room / (amount < 1 ? 1 : amount);
        }

        /// <summary>What to do with a drag of packages onto a mixing station's product slot.</summary>
        public enum DropAction
        {
            /// <summary>Let the game's own drop run as it is.</summary>
            Allow,
            /// <summary>Cut the dragged amount to <c>count</c> packages, then let the game drop.</summary>
            Cut,
            /// <summary>Unpack <c>count</c> packages into the stack already in the slot; the game's drop is cancelled.</summary>
            Merge,
            /// <summary>Do nothing: the game's drop is cancelled.</summary>
            Cancel,
        }

        /// <summary>
        /// Decides a drop. <c>fit</c> is the whole packages the slot has room for, <c>dragging</c> the packages
        /// being dragged. A slot that already holds a stack takes a merge (the game would swap the stacks); an
        /// empty slot takes the drop, cut to what fits (the game does not check room there).
        /// </summary>
        public static DropAction PlanDrop(bool slotHasStack, int fit, int dragging, out int count)
        {
            count = fit < dragging ? fit : dragging;
            if (count < 0) count = 0;
            if (count == 0) return DropAction.Cancel;
            if (slotHasStack) return DropAction.Merge;
            return dragging > fit ? DropAction.Cut : DropAction.Allow;
        }
    }
}
