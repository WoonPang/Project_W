using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyHealthScalingValidation
{
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private static readonly string[] PrefabPaths =
    {
        "Assets/04. Prefabs/Enemies/BasicMeleeEnemy.prefab",
        "Assets/04. Prefabs/Enemies/TankMeleeEnemy.prefab",
        "Assets/04. Prefabs/Enemies/BasicRangedEnemy.prefab",
        "Assets/04. Prefabs/Enemies/AssassinMeleeEnemy.prefab",
        "Assets/04. Prefabs/Enemies/ChargingRangedEnemy.prefab",
        "Assets/04. Prefabs/Enemies/MidBoss.prefab",
        "Assets/04. Prefabs/Enemies/FinalBoss.prefab"
    };

    public static void ValidateAll()
    {
        Validate();
        PrototypePlaySetup.Validate();
        BasicRangedEnemySetup.Validate();
        TankMeleeEnemySetup.Validate();
        AssassinMeleeEnemySetup.Validate();
        ChargingRangedEnemySetup.Validate();
        MidBossSetup.Validate();
        FinalBossSetup.Validate();
        Debug.Log("All enemy and boss validations passed with level-scaled health.");
    }

    public static void Validate()
    {
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.BasicMelee, 17), 100f);
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.TankMelee, 17), 162.5f);
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.BasicRanged, 17), 75f);
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.AssassinMelee, 17), 50f);
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.ChargingRanged, 17), 50f);
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.MidBoss, 17), 750f);
        AssertHealth(EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.FinalBoss, 17), 2500f);

        EditorSceneManager.OpenScene(ScenePath);
        PlayerStats stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        PlayerProgress progress = stats.GetComponent<PlayerProgress>();
        InvokeAwake(stats);
        InvokeAwake(progress);
        GameObject[] instances = new GameObject[PrefabPaths.Length];
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            for (int index = 0; index < PrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[index]);
                instances[index] = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instances[index].transform.position = stats.transform.position +
                    Vector3.right * (index + 4f);
                InvokeAwake(instances[index].GetComponent<CombatTarget2D>());
            }
            AssertHealth(instances[0].GetComponent<BasicMeleeEnemy2D>().MaxHealth, 20f);
            AssertHealth(instances[1].GetComponent<TankMeleeEnemy2D>().MaxHealth, 32.5f);
            AssertHealth(instances[2].GetComponent<BasicRangedEnemy2D>().MaxHealth, 15f);
            AssertHealth(instances[3].GetComponent<AssassinMeleeEnemy2D>().MaxHealth, 10f);
            AssertHealth(instances[4].GetComponent<ChargingRangedEnemy2D>().MaxHealth, 10f);
            AssertHealth(instances[5].GetComponent<MidBoss2D>().MaxHealth, 150f);
            AssertHealth(instances[6].GetComponent<FinalBoss2D>().MaxHealth, 500f);

            BasicMeleeEnemy2D melee = instances[0].GetComponent<BasicMeleeEnemy2D>();
            melee.TakeDamage(5f);
            progress.LevelUpImmediately();
            if (progress.CurrentLevel != 2)
                throw new InvalidOperationException("Player level did not advance.");
            AssertHealth(melee.MaxHealth, 25f);
            AssertHealth(melee.CurrentHealth, 18.75f);
            AssertHealth(instances[1].GetComponent<TankMeleeEnemy2D>().MaxHealth, 40.625f);
            AssertHealth(instances[2].GetComponent<BasicRangedEnemy2D>().MaxHealth, 18.75f);
            AssertHealth(instances[3].GetComponent<AssassinMeleeEnemy2D>().MaxHealth, 12.5f);
            AssertHealth(instances[4].GetComponent<ChargingRangedEnemy2D>().MaxHealth, 12.5f);
            AssertHealth(instances[5].GetComponent<MidBoss2D>().MaxHealth, 187.5f);
            AssertHealth(instances[6].GetComponent<FinalBoss2D>().MaxHealth, 625f);

            stats.AddAttackDamage(5f);
            progress.LevelUpImmediately();
            AssertHealth(stats.AttackDamage, 20f);
            AssertHealth(melee.MaxHealth, 30f);
            AssertHealth(melee.CurrentHealth, 22.5f);
            AssertHealth(instances[1].GetComponent<TankMeleeEnemy2D>().MaxHealth, 48.75f);
            AssertHealth(instances[2].GetComponent<BasicRangedEnemy2D>().MaxHealth, 22.5f);
            AssertHealth(instances[3].GetComponent<AssassinMeleeEnemy2D>().MaxHealth, 15f);
            AssertHealth(instances[4].GetComponent<ChargingRangedEnemy2D>().MaxHealth, 15f);
            AssertHealth(instances[5].GetComponent<MidBoss2D>().MaxHealth, 225f);
            AssertHealth(instances[6].GetComponent<FinalBoss2D>().MaxHealth, 750f);

            GameObject fresh = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[0]));
            try
            {
                InvokeAwake(fresh.GetComponent<BasicMeleeEnemy2D>());
                AssertHealth(fresh.GetComponent<BasicMeleeEnemy2D>().MaxHealth, 30f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fresh);
            }
            Debug.Log("Enemy health follows baseline level damage, preserves live health ratio, and ignores attack augments.");
        }
        finally
        {
            foreach (GameObject instance in instances)
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            Time.timeScale = previousTimeScale;
        }
    }

    private static void InvokeAwake(Component component)
    {
        MethodInfo awake = component.GetType().GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (awake == null)
            throw new InvalidOperationException("Missing Awake on " + component.GetType().Name);
        awake.Invoke(component, null);
    }

    private static void AssertHealth(float actual, float expected)
    {
        if (Mathf.Abs(actual - expected) > 0.001f)
            throw new InvalidOperationException($"Expected HP {expected}, got {actual}.");
    }
}
