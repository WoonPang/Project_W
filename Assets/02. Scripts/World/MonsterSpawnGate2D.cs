public static class MonsterSpawnGate2D
{
    public static bool IsPaused { get; private set; }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
    }
}
