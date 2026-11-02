using System;
using UnityEngine;

public static class AugmentRangeValidation
{
    public static void Validate()
    {
        if (AugmentCatalog.All.Count != 13 ||
            !Mathf.Approximately(AugmentCatalog.AutomaticSkillRangeMultiplier(0), 1f) ||
            !Mathf.Approximately(AugmentCatalog.AutomaticSkillRangeMultiplier(5), 1.5f))
            throw new InvalidOperationException("Range augment catalog or multiplier is incorrect.");

        GameObject testPlayer = new GameObject("Range Augment Validation");
        try
        {
            PlayerAugments augments = testPlayer.AddComponent<PlayerAugments>();
            PlayerAutomaticAugments2D skills = testPlayer.AddComponent<PlayerAutomaticAugments2D>();

            if (augments.CanUpgrade(AugmentId.EnemySlowRange) ||
                augments.CanUpgrade(AugmentId.EnemyStunRange))
                throw new InvalidOperationException("Range cards appeared before their skills were unlocked.");
            CheckOffersHideLockedRangeCards(augments, true, true);

            augments.Apply(AugmentId.EnemySlow);
            CheckOffersHideLockedRangeCards(augments, false, true);
            augments.Apply(AugmentId.EnemyStun);
            if (!augments.CanUpgrade(AugmentId.EnemySlowRange) ||
                !augments.CanUpgrade(AugmentId.EnemyStunRange))
                throw new InvalidOperationException("Range cards are unavailable after skill unlock.");

            for (int level = 1; level <= 5; level++)
            {
                augments.Apply(AugmentId.EnemySlowRange);
                augments.Apply(AugmentId.EnemyStunRange);
                float expectedRadius = 3f * (1f + 0.1f * level);
                if (!Mathf.Approximately(skills.SlowEffectRadius, expectedRadius) ||
                    !Mathf.Approximately(skills.StunEffectRadius, expectedRadius))
                    throw new InvalidOperationException("Skill radius did not grow at level " + level);
            }

            if (augments.CanUpgrade(AugmentId.EnemySlowRange) ||
                augments.CanUpgrade(AugmentId.EnemyStunRange))
                throw new InvalidOperationException("Max-level range cards remained available.");

            if (AugmentCatalog.Get(AugmentId.EnemySlow).DescriptionForNextLevel(1) !=
                    "쿨타임 20초 → 19초" ||
                AugmentCatalog.Get(AugmentId.EnemyStun).DescriptionForNextLevel(1) !=
                    "쿨타임 15초 → 14초" ||
                AugmentCatalog.Get(AugmentId.EnemySlowRange).DescriptionForNextLevel(4) !=
                    "범위 +10%" ||
                AugmentCatalog.Get(AugmentId.EnemyStunRange).DescriptionForNextLevel(4) !=
                    "범위 +10%")
                throw new InvalidOperationException("Range or cooldown card text is incorrect.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testPlayer);
        }

        Debug.Log("Slow and stun range augment validation passed.");
    }

    private static void CheckOffersHideLockedRangeCards(PlayerAugments augments,
        bool slowLocked, bool stunLocked)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            foreach (AugmentId id in augments.RollOffers())
            {
                if ((slowLocked && id == AugmentId.EnemySlowRange) ||
                    (stunLocked && id == AugmentId.EnemyStunRange))
                    throw new InvalidOperationException("A locked range card appeared in level-up offers.");
            }
        }
    }
}
