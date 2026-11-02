namespace WarehouseAccess
{
    /// <summary>The one question every patch asks: should the warehouse be open right now?</summary>
    internal static class WarehouseGate
    {
        public static bool ShouldOpen()
            => Access.ShouldOpen(
                ModConfig.SkipQuest || GameAccess.WarehouseQuestCompleted(),
                ModConfig.AlwaysOpen,
                !ModConfig.AlwaysOpen && GameAccess.RankMet());
    }
}
