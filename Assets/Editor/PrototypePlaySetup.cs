using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrototypePlaySetup
{
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string EnemySpritePath = "Assets/03. Asset/Sprites/BasicMeleeEnemy.png";
    private const string EnemyPrefabPath = "Assets/04. Prefabs/Enemies/BasicMeleeEnemy.prefab";

    public static void Create()
    {
        Sprite sprite = CreateWhiteSquareSprite();
        BasicMeleeEnemy2D enemyPrefab = CreateEnemyPrefab(sprite);

        Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        foreach (TrainingDummy2D dummy in UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
                     FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(dummy.gameObject);

        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (camera == null || player == null || hud == null)
            throw new InvalidOperationException("The test scene needs a camera, player, and HUD.");

        // Test-only HUD controls are scene additions. Remove them from the play copy.
        foreach (Button button in hud.GetComponentsInChildren<Button>(true))
        {
            if (button == null)
                continue;
            Transform parent = button.transform.parent;
            UnityEngine.Object.DestroyImmediate(parent != null && parent.name.Contains("Border")
                ? parent.gameObject : button.gameObject);
        }
        ExperiencePickupSpawner pickupSpawner = hud.GetComponent<ExperiencePickupSpawner>();
        if (pickupSpawner != null)
            UnityEngine.Object.DestroyImmediate(pickupSpawner);

        GameObject spawnerObject = new GameObject("Basic Melee Enemy Spawner");
        spawnerObject.AddComponent<BasicMeleeEnemySpawner2D>()
            .Configure(enemyPrefab, camera, player);

        if (!EditorSceneManager.SaveScene(scene, PlayScenePath))
            throw new InvalidOperationException("Prototype_Play could not be saved.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SetBuildScenes();
        Validate();
        BasicRangedEnemySetup.Create();
        Debug.Log("Prototype_Test preserved; Prototype_Play and both basic enemies are ready.");
    }

    private static Sprite CreateWhiteSquareSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath);
        if (existing != null)
            return existing;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int index = 0; index < pixels.Length; index++)
            pixels[index] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(EnemySpritePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(EnemySpritePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(EnemySpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath);
    }

    private static BasicMeleeEnemy2D CreateEnemyPrefab(Sprite sprite)
    {
        Directory.CreateDirectory("Assets/04. Prefabs/Enemies");
        if (sprite == null)
            throw new InvalidOperationException("The enemy sprite is missing.");

        GameObject enemyObject = new GameObject("BasicMeleeEnemy");
        try
        {
            SpriteRenderer body = enemyObject.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = 9;

            Rigidbody2D rigidbody = enemyObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            BoxCollider2D collider = enemyObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.86f;
            collider.isTrigger = true;
            enemyObject.AddComponent<EnemyStatus2D>();

            SpriteRenderer background = CreateHealthBarPart(enemyObject.transform,
                "Health Bar Background", sprite, new Color(0.08f, 0.12f, 0.15f),
                new Vector3(0f, 0.66f, -0.01f), new Vector3(1.02f, 0.14f, 1f), 11);
            SpriteRenderer fill = CreateHealthBarPart(enemyObject.transform,
                "Health Bar Fill", sprite, new Color(0.28f, 0.9f, 0.42f),
                new Vector3(0f, 0.66f, -0.02f), new Vector3(0.94f, 0.08f, 1f), 12);
            if (background == null)
                throw new InvalidOperationException("The enemy health bar could not be created.");

            enemyObject.AddComponent<BasicMeleeEnemy2D>().Configure(body, fill);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(enemyObject, EnemyPrefabPath);
            return saved.GetComponent<BasicMeleeEnemy2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemyObject);
        }
    }

    private static SpriteRenderer CreateHealthBarPart(Transform parent, string name,
        Sprite sprite, Color color, Vector3 position, Vector3 scale, int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localScale = scale;
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void SetBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(PlayScenePath, true),
            new EditorBuildSettingsScene(TestScenePath, false)
        };
        foreach (EditorBuildSettingsScene old in EditorBuildSettings.scenes)
        {
            if (old.path != "Assets/01. Scenes/Prototype.unity" &&
                old.path != PlayScenePath && old.path != TestScenePath)
                scenes.Add(old);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    public static void Validate()
    {
        Scene play = EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        if (!play.IsValid() ||
            UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("The play scene still has test dummies.");

        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        BasicMeleeEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<BasicMeleeEnemySpawner2D>();
        if (camera == null || player == null || spawner == null ||
            UnityEngine.Object.FindFirstObjectByType<LevelUpUI>() == null ||
            UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>() == null)
            throw new InvalidOperationException("Prototype_Play is missing a game system.");
        if (spawner.MaximumAlive != 40 ||
            new SerializedObject(spawner).FindProperty("enemyPrefab").objectReferenceValue !=
            AssetDatabase.LoadAssetAtPath<BasicMeleeEnemy2D>(EnemyPrefabPath))
            throw new InvalidOperationException("The play scene has an invalid enemy spawner reference.");
        foreach (Button button in UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>()
                     .GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.activeSelf)
                throw new InvalidOperationException("A prototype test button is visible in the play scene.");
        }

        for (int edge = 0; edge < 4; edge++)
        for (int index = 0; index < 3; index++)
        {
            Vector2 position = BasicMeleeEnemySpawner2D.PositionOutsideView(camera,
                player.transform.position, 1.5f, 7f, edge, index * 0.5f);
            Vector3 viewport = camera.WorldToViewportPoint(position);
            if (viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f ||
                Vector2.Distance(position, player.transform.position) < 7f)
                throw new InvalidOperationException("An enemy could spawn on-screen or near the player.");
        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        try
        {
            BasicMeleeEnemy2D enemy = prefab.GetComponent<BasicMeleeEnemy2D>();
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
            if (enemy == null || enemy.MaxHealth != 30f || enemy.AttackDamage != 5f ||
                enemy.MoveSpeed != 0.9f || enemy.ExperienceOnDeath != 5f ||
                renderer == null || renderer.color != Color.white ||
                collider == null || !collider.isTrigger)
                throw new InvalidOperationException("Basic melee enemy prefab values do not match the PDF.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(FindObjectsSortMode.None).Length != 4)
            throw new InvalidOperationException("Prototype_Test was not preserved.");
        EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        ValidateCombatTarget();
        Debug.Log("Play/test scenes, PDF enemy values, and off-screen spawning validated.");
    }

    private static void ValidateCombatTarget()
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        try
        {
            PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            instance.transform.position = player.transform.position + Vector3.right * 2f;
            BasicMeleeEnemy2D enemy = instance.GetComponent<BasicMeleeEnemy2D>();
            MethodInfo awake = typeof(BasicMeleeEnemy2D).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (awake == null)
                throw new InvalidOperationException("Enemy initialization is missing.");
            awake.Invoke(enemy, null);
            Physics2D.SyncTransforms();

            RSkillCooldown barrage = player.GetComponent<RSkillCooldown>();
            AutoAttack2D autoAttack = player.GetComponent<AutoAttack2D>();
            MethodInfo findNearest = typeof(AutoAttack2D).GetMethod("FindNearestTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (barrage == null || autoAttack == null || findNearest == null ||
                barrage.FindNearestVisibleTarget() != enemy ||
                findNearest.Invoke(autoAttack, null) != enemy)
                throw new InvalidOperationException("Player attacks do not select the new enemy.");

            float reportedDamage = 0f;
            Action<float, Vector3> onDamage = (damage, _) => reportedDamage = damage;
            CombatTarget2D.Damaged += onDamage;
            try
            {
                enemy.TakeDamage(10f);
            }
            finally
            {
                CombatTarget2D.Damaged -= onDamage;
            }
            if (!enemy.IsAlive || enemy.CurrentHealth != 20f || reportedDamage != 10f)
                throw new InvalidOperationException("Enemy health or damage popups are not connected.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
