namespace TeleportApp
{
    /// <summary>Whether a place is offered, given the settings. Game-free, so unit-tested.</summary>
    internal static class Filter
    {
        /// <summary>
        /// A place is offered when its kind is switched on and, if the kind has an owned/hired state
        /// and the setting asks for it, the place has that state.
        /// </summary>
        public static bool Offers(bool kindEnabled, bool onlyIfOwned, bool owned)
            => kindEnabled && (!onlyIfOwned || owned);
    }
}
