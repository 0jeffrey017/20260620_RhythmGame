namespace RhythmGame
{
    /// <summary>
    /// Survives scene reloads (static) so Retry can replay the same difficulty
    /// without showing the selection screen again.
    /// </summary>
    public static class GameSession
    {
        public static string SelectedChart;

        public static bool HasSelection => !string.IsNullOrEmpty(SelectedChart);

        public static void Clear() => SelectedChart = null;
    }
}
