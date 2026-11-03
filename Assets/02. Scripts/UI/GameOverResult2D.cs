using UnityEngine;

public readonly struct GameOverResult2D
{
    public readonly int Level;
    public readonly int Seconds;
    public readonly int Kills;
    public readonly int LevelScore;
    public readonly int TimeScore;
    public readonly int KillScore;
    public readonly int TotalScore;
    public readonly int Coins;

    public string DisplayTime => $"{Seconds / 60:00}:{Seconds % 60:00}";

    public GameOverResult2D(int level, float elapsedSeconds, int kills)
    {
        Level = Mathf.Max(1, level);
        Seconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        Kills = Mathf.Max(0, kills);
        LevelScore = Level * 15;
        TimeScore = Seconds * 3;
        KillScore = Kills * 2;
        TotalScore = LevelScore + TimeScore + KillScore;
        Coins = CoinsFromScore(TotalScore);
    }

    public static int CoinsFromScore(int score)
    {
        return Mathf.Max(0, Mathf.CeilToInt(score / 10f));
    }
}
