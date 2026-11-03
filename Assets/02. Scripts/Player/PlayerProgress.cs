using System;
using UnityEngine;

public sealed class PlayerProgress : MonoBehaviour
{
    [SerializeField, Min(1)] private int startingLevel = 1;
    [SerializeField, Min(0f)] private float startingExperience = 0f;

    public int CurrentLevel { get; private set; }
    public float CurrentExperience { get; private set; }
    public float ExperienceToNextLevel => ExperienceRequiredForLevel(CurrentLevel);
    public float ExperienceRemaining => Mathf.Max(0f, ExperienceToNextLevel - CurrentExperience);
    public float ExperiencePercent => ExperienceToNextLevel > 0f
        ? CurrentExperience / ExperienceToNextLevel * 100f : 0f;
    public event Action<int> LeveledUp;

    public static float ExperienceRequiredForLevel(int level)
    {
        level = Mathf.Max(1, level);
        if (level <= 10)
            return 100f + 10f * (level - 1);
        if (level <= 25)
            return 190f + 20f * (level - 10);

        float levelsSince25 = level - 25f;
        return 490f + 40f * levelsSince25 + 10f * levelsSince25 * levelsSince25;
    }

    private void Awake()
    {
        ResetProgress();
    }

    public void ResetProgress()
    {
        CurrentLevel = Mathf.Max(1, startingLevel);
        CurrentExperience = 0f;
        AddExperience(startingExperience);
        EnemyHealthScaling2D.SyncExistingEnemies(CurrentLevel);
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentExperience += amount;
        while (CurrentExperience >= ExperienceToNextLevel)
        {
            CurrentExperience -= ExperienceToNextLevel;
            AdvanceLevel();
        }
    }

    // Used by the Test scene's EXP +10% button.
    public void AddExperiencePercent(float percent)
    {
        if (percent > 0f)
            AddExperience(ExperienceToNextLevel * percent / 100f);
    }

    public void LevelUpImmediately()
    {
        CurrentExperience = 0f;
        AdvanceLevel();
    }

    private void AdvanceLevel()
    {
        CurrentLevel++;
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.ApplyLevelGrowth();
        EnemyHealthScaling2D.SyncExistingEnemies(CurrentLevel);
        LeveledUp?.Invoke(CurrentLevel);
    }
}
