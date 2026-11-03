namespace MixPackagedDrugs
{
    /// <summary>
    /// The arithmetic of packaged product. A package holds <c>amount</c> units (bag 1, jar 5, brick 20),
    /// and the mixing station works in units: one mixer for each.
    /// </summary>
    public static class Packages
    {
        public const int BagUnits = 1;
        public const int JarUnits = 5;
        public const int BrickUnits = 20;

        /// <summary>Units in a stack of packages. A zero or negative amount counts as 1 (unpackaged).</summary>
        public static int Units(int quantity, int amount)
        {
            if (quantity <= 0) return 0;
            return quantity * (amount < 1 ? 1 : amount);
        }

        /// <summary>
        /// Whole packages the product slot can still take. The slot holds up to <c>stackLimit</c> units and
        /// already has <c>current</c>; a package is never split, so a brick that does not fully fit is refused.
        /// </summary>
        public static int PackagesThatFit(int stackLimit, int current, int amount)
        {
            int room = stackLimit - current;
            if (room <= 0) return 0;
            return room / (amount < 1 ? 1 : amount);
        }

        /// <summary>
        /// Packages a mixed batch of <c>units</c> becomes when put back in packages of <c>amount</c> units:
        /// 20 units in bricks is 1, 10 units in jars is 2. 0 when the batch does not divide evenly, because a
        /// package is never part full; the batch then stays loose.
        /// </summary>
        public static int RepackQuantity(int units, int amount)
        {
            if (units <= 0 || amount < 1) return 0;
            return units % amount == 0 ? units / amount : 0;
        }

        /// <summary>
        /// Whether the settings allow a package of this size. The three sizes the game ships have a switch;
        /// any other size follows <c>allowOther</c>.
        /// </summary>
        public static bool IsAllowed(int amount, bool bags, bool jars, bool bricks, bool allowOther)
        {
            switch (amount)
            {
                case BagUnits: return bags;
                case JarUnits: return jars;
                case BrickUnits: return bricks;
                default: return allowOther;
            }
        }
    }
}
