using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AugmentSetup
{
    private const string PlayerPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string MenuPath = "Assets/04. Prefabs/UI/LevelUpMenu.prefab";
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test.unity";

    public static void Apply()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            if (player.GetComponent<PlayerAugments>() == null)
                player.AddComponent<PlayerAugments>();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }

        GameObject menu = PrefabUtility.LoadPrefabContents(MenuPath);
        try
        {
            Transform overlay = menu.transform.Find("Level Up Overlay");
            Button[] cards = overlay.GetComponentsInChildren<Button>(true);
            if (cards.Length != 3)
                throw new InvalidOperationException("The level-up menu needs exactly three cards.");
            for (int index = 0; index < cards.Length; index++)
            {
                cards[index].name = "Augment Card " + (index + 1);
                Text title = cards[index].transform.Find("Title").GetComponent<Text>();
                Text description = cards[index].transform.Find("Description").GetComponent<Text>();
                title.text = "증강 " + (index + 1);
                title.fontSize = 32;
                description.text = "레벨업 시 무작위로 표시됩니다.";
                description.fontSize = 26;
            }
            menu.GetComponent<LevelUpUI>().Configure(overlay.gameObject,
                cards[0], cards[1], cards[2]);
            PrefabUtility.SaveAsPrefabAsset(menu, MenuPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(menu);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath);
        Validate();
        Debug.Log("PDF augment catalog, random offers, and available effects are ready.");
    }

    private static void Validate()
    {
        if (AugmentCatalog.All.Count != 13)
            throw new InvalidOperationException("The prototype needs thirteen augment types.");

        PlayerAugments player = UnityEngine.Object.FindFirstObjectByType<PlayerAugments>();
        LevelUpUI menu = UnityEngine.Object.FindFirstObjectByType<LevelUpUI>();
        if (player == null || menu == null)
            throw new InvalidOperationException("Player augments or level-up UI is missing from Prototype.");

        GameObject testPlayer = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));
        try
        {
            PlayerAugments augments = testPlayer.GetComponent<PlayerAugments>();
            PlayerStats stats = testPlayer.GetComponent<PlayerStats>();
            RSkillCooldown barrage = testPlayer.GetComponent<RSkillCooldown>();
            PlayerMovement2D movement = testPlayer.GetComponent<PlayerMovement2D>();

            for (int count = 0; count < 100; count++)
                ValidateOffers(augments, true, 3);

            augments.Apply(AugmentId.BarrageBullets);
            augments.Apply(AugmentId.BarrageDamage);
            augments.Apply(AugmentId.BarrageCooldown);
            augments.Apply(AugmentId.DashCooldown);
            augments.Apply(AugmentId.MaxHealth);
            augments.Apply(AugmentId.AttackDamage);
            augments.Apply(AugmentId.MoveSpeed);
            augments.Apply(AugmentId.AttackSpeed);
            if (barrage.TotalShots != 60 || !Mathf.Approximately(barrage.DamageMultiplier, 1.6f) ||
                !Mathf.Approximately(barrage.CooldownSeconds, 27.5f) ||
                !Mathf.Approximately(movement.DashCooldownSeconds, 9f) ||
                !Mathf.Approximately(stats.MaxHealth, 105f) ||
                !Mathf.Approximately(stats.AttackDamage, 12.5f) ||
                !Mathf.Approximately(stats.MoveSpeed, 1.05f) ||
                !Mathf.Approximately(stats.AttackSpeed, 1.1f))
                throw new InvalidOperationException("Implemented augment effects have wrong values.");

            foreach (AugmentDefinition definition in AugmentCatalog.All)
            {
                if (definition.MaxLevel == int.MaxValue)
                    continue;
                while (augments.CanUpgrade(definition.Id))
                    augments.Apply(definition.Id);
                if (augments.GetLevel(definition.Id) != definition.MaxLevel)
                    throw new InvalidOperationException("An augment did not stop at its maximum level.");
            }
            if (barrage.TotalShots != 100 || !Mathf.Approximately(barrage.DamageMultiplier, 2f) ||
                !Mathf.Approximately(barrage.CooldownSeconds, 15f) ||
                !Mathf.Approximately(movement.DashCooldownSeconds, 5f) ||
                !Mathf.Approximately(stats.MoveSpeed, 1.25f) ||
                !Mathf.Approximately(stats.AttackSpeed, 2f) ||
                !Mathf.Approximately(AugmentCatalog.AutomaticSkillCooldown(AugmentId.Shield, 5), 20f) ||
                !Mathf.Approximately(AugmentCatalog.AutomaticSkillCooldown(AugmentId.EnemySlow, 5), 15f) ||
                !Mathf.Approximately(AugmentCatalog.AutomaticSkillCooldown(AugmentId.EnemyStun, 5), 10f))
                throw new InvalidOperationException("Maximum augment values do not match the design.");

            for (int count = 0; count < 30; count++)
                ValidateOffers(augments, false, 2);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testPlayer);
        }
        Debug.Log("Thirteen augments, offer rules, max levels, and applied stats validated.");
    }

    private static void ValidateOffers(PlayerAugments augments, bool requireNonStat, int expectedCount)
    {
        AugmentId[] offers = augments.RollOffers();
        if (offers.Length != expectedCount)
            throw new InvalidOperationException("An offer set has the wrong size.");
        for (int first = 0; first < offers.Length; first++)
        for (int second = first + 1; second < offers.Length; second++)
        {
            if (offers[first] == offers[second])
                throw new InvalidOperationException("An offer set has duplicates.");
        }
        int statCount = 0;
        foreach (AugmentId id in offers)
        {
            if (!augments.CanUpgrade(id))
                throw new InvalidOperationException("A max-level augment appeared in an offer.");
            if (AugmentCatalog.Get(id).Category == AugmentCategory.Stat)
                statCount++;
        }
        if (requireNonStat && statCount > 2)
            throw new InvalidOperationException("Three stat augments appeared before core max levels.");
        if (!requireNonStat && statCount != expectedCount)
            throw new InvalidOperationException("Only stat augments should remain after all other max levels.");
    }
}
