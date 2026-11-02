using System;
using UnityEngine;

public static class ExperienceFormulaValidation
{
    public static void Validate()
    {
        CheckRequirement(1, 100f);
        CheckRequirement(10, 190f);
        CheckRequirement(11, 210f);
        CheckRequirement(25, 490f);
        CheckRequirement(26, 540f);
        CheckRequirement(27, 610f);
        CheckRequirement(30, 940f);

        GameObject testObject = new GameObject("Experience Formula Validation");
        try
        {
            PlayerStats stats = testObject.AddComponent<PlayerStats>();
            PlayerProgress progress = testObject.AddComponent<PlayerProgress>();
            progress.ResetProgress();
            progress.AddExperience(7.5f);
            CheckProgress(progress, 1, 7.5f, 92.5f);

            progress.AddExperience(92.5f);
            CheckProgress(progress, 2, 0f, 110f);

            progress.AddExperience(330.5f);
            CheckProgress(progress, 4, 100.5f, 29.5f);

            progress.AddExperiencePercent(10f);
            CheckProgress(progress, 4, 113.5f, 16.5f);

            progress.LevelUpImmediately();
            CheckProgress(progress, 5, 0f, 140f);
            if (!Mathf.Approximately(stats.MaxHealth, 160f) ||
                !Mathf.Approximately(stats.AttackDamage, 20f) ||
                !Mathf.Approximately(stats.MoveSpeed, 1.1f) ||
                !Mathf.Approximately(stats.AttackSpeed, 1.1f))
                throw new InvalidOperationException("Level-up base stat growth was not applied on every level.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }

        Debug.Log("Experience formula and rollover validation passed.");
    }

    private static void CheckRequirement(int level, float expected)
    {
        if (!Mathf.Approximately(PlayerProgress.ExperienceRequiredForLevel(level), expected))
            throw new InvalidOperationException("Wrong EXP requirement at level " + level);
    }

    private static void CheckProgress(PlayerProgress progress, int level, float experience, float remaining)
    {
        if (progress.CurrentLevel != level ||
            !Mathf.Approximately(progress.CurrentExperience, experience) ||
            !Mathf.Approximately(progress.ExperienceRemaining, remaining))
            throw new InvalidOperationException("EXP rollover failed at level " + level);
    }
}
