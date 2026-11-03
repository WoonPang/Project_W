using System;
using UnityEngine;

public static class AugmentProgressionValidation
{
    public static void ValidateAll()
    {
        ExperienceFormulaValidation.Validate();
        Validate();
        AugmentSetup.Apply();
    }

    public static void Validate()
    {
        if (AugmentCatalog.Get(AugmentId.Shield).Title != "보호막" ||
            AugmentCatalog.Get(AugmentId.Shield).DescriptionForNextLevel(1) !=
                "쿨타임 30초 → 27.5초" ||
            AugmentCatalog.Get(AugmentId.EnemyStun).DescriptionForNextLevel(0) !=
                "3초 뒤 시전 · 1.5초 기절\n쿨타임 15초")
            throw new InvalidOperationException("Automatic skill card text is incorrect.");

        GameObject testPlayer = new GameObject("Augment Progression Validation");
        try
        {
            PlayerAugments augments = testPlayer.AddComponent<PlayerAugments>();
            PlayerStats stats = testPlayer.GetComponent<PlayerStats>();

            foreach (AugmentDefinition definition in AugmentCatalog.All)
            {
                if (definition.MaxLevel == int.MaxValue)
                    continue;
                if (definition.LabelForNextLevel(definition.MaxLevel - 2) !=
                    "Lv" + (definition.MaxLevel - 1) ||
                    definition.LabelForNextLevel(definition.MaxLevel - 1) != "Lv.MAX")
                    throw new InvalidOperationException("The last limited card should display Lv.MAX.");
                while (augments.CanUpgrade(definition.Id))
                    augments.Apply(definition.Id);
                if (augments.GetLevel(definition.Id) != definition.MaxLevel)
                    throw new InvalidOperationException("A limited augment did not stop at its maximum level.");
            }

            if (!Mathf.Approximately(stats.MoveSpeed, 1.25f) ||
                !Mathf.Approximately(stats.AttackSpeed, 2f))
                throw new InvalidOperationException("Speed augment values do not match their caps.");

            AugmentId[] offers = augments.RollOffers();
            if (offers.Length != 2 || offers[0] == offers[1] ||
                (offers[0] != AugmentId.MaxHealth && offers[0] != AugmentId.AttackDamage) ||
                (offers[1] != AugmentId.MaxHealth && offers[1] != AugmentId.AttackDamage))
                throw new InvalidOperationException("Late-game offers should contain two unique stat cards.");

            for (int count = 0; count < 6; count++)
            {
                augments.Apply(AugmentId.MaxHealth);
                augments.Apply(AugmentId.AttackDamage);
            }
            if (augments.GetLevel(AugmentId.MaxHealth) != 6 ||
                augments.GetLevel(AugmentId.AttackDamage) != 6 ||
                AugmentCatalog.Get(AugmentId.MaxHealth).LabelForNextLevel(5) != "Lv6" ||
                AugmentCatalog.Get(AugmentId.AttackDamage).LabelForNextLevel(5) != "Lv6" ||
                !augments.CanUpgrade(AugmentId.MaxHealth) ||
                !augments.CanUpgrade(AugmentId.AttackDamage))
                throw new InvalidOperationException("Repeatable stat levels did not continue past Lv5.");

            PlayerProgress progress = testPlayer.AddComponent<PlayerProgress>();
            progress.ResetProgress();
            for (int count = 0; count < 29; count++)
                progress.LevelUpImmediately();
            if (progress.CurrentLevel != 30 ||
                !Mathf.Approximately(stats.MaxHealth, 275f) ||
                !Mathf.Approximately(stats.AttackDamage, 97.5f) ||
                Mathf.Abs(stats.MoveSpeed - 1.975f) > 0.0001f ||
                Mathf.Abs(stats.AttackSpeed - 2.725f) > 0.0001f)
                throw new InvalidOperationException("Character levels should keep growing after speed augments max out.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testPlayer);
        }

        Debug.Log("Augment names, cooldown text, caps, and repeatable levels validated.");
    }
}
