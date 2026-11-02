using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BasicRangedEnemySetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string SpritePath = "Assets/03. Asset/Sprites/BasicRangedEnemy.png";
    private const string EnemyPrefabPath = "Assets/04. Prefabs/Enemies/BasicRangedEnemy.prefab";
    private const string ProjectilePrefabPath = "Assets/04. Prefabs/Projectiles/EnemyProjectile.prefab";
    private const string HudPrefabPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string MeleePrefabPath = "Assets/04. Prefabs/Enemies/BasicMeleeEnemy.prefab";

    public static void Create()
    {
        Sprite circle = CreateCircleSprite();
        EnemyProjectile2D projectile = CreateProjectilePrefab(circle);
        BasicRangedEnemy2D enemy = CreateEnemyPrefab(circle, projectile);
        RemoveDebugHudButtons();
        SaveMeleePrefabWithoutPickup();

        Scene scene = EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        if (camera == null || player == null)
            throw new InvalidOperationException("Prototype_Play needs a camera and player.");
        BasicRangedEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<BasicRangedEnemySpawner2D>();
        if (spawner == null)
            spawner = new GameObject("Basic Ranged Enemy Spawner")
                .AddComponent<BasicRangedEnemySpawner2D>();
        spawner.Configure(enemy, camera, player);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Direct monster EXP, cleaned HUD, and basic ranged enemy are ready.");
    }

    private static Sprite CreateCircleSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null)
            return existing;
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - size / 2f;
            float dy = y + 0.5f - size / 2f;
            pixels[y * size + x] = dx * dx + dy * dy <= 30f * 30f
                ? new Color32(255, 255, 255, 255)
                : new Color32(255, 255, 255, 0);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(SpritePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static EnemyProjectile2D CreateProjectilePrefab(Sprite sprite)
    {
        GameObject projectile = new GameObject("EnemyProjectile");
        try
        {
            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.78f, 0.48f);
            renderer.sortingOrder = 11;
            projectile.transform.localScale = Vector3.one * 0.28f;
            projectile.AddComponent<EnemyProjectile2D>();
            return PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath)
                .GetComponent<EnemyProjectile2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(projectile);
        }
    }

    private static BasicRangedEnemy2D CreateEnemyPrefab(Sprite sprite,
        EnemyProjectile2D projectile)
    {
        GameObject enemy = new GameObject("BasicRangedEnemy");
        try
        {
            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 9;
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = 0.46f;
            collider.isTrigger = true;
            enemy.AddComponent<EnemyStatus2D>();
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/03. Asset/Sprites/BasicMeleeEnemy.png");
            CreateHealthBarPart(enemy.transform, "Health Bar Background", square,
                new Color(0.08f, 0.12f, 0.15f), 1.02f, 0.14f, 11);
            SpriteRenderer fill = CreateHealthBarPart(enemy.transform, "Health Bar Fill",
                square, new Color(0.28f, 0.9f, 0.42f), 0.94f, 0.08f, 12);
            enemy.AddComponent<BasicRangedEnemy2D>().Configure(renderer, fill, projectile);
            return PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath)
                .GetComponent<BasicRangedEnemy2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemy);
        }
    }

    private static SpriteRenderer CreateHealthBarPart(Transform parent, string name,
        Sprite sprite, Color color, float width, float height, int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, 0.66f, sortingOrder == 11 ? -0.01f : -0.02f);
        child.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void RemoveDebugHudButtons()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            PlayerHudUI ui = hud.GetComponent<PlayerHudUI>();
            if (ui == null)
                throw new InvalidOperationException("PlayerHUD component is missing.");
            SerializedObject serialized = new SerializedObject(ui);
            foreach (string field in new[] { "healthPlusButton", "healthMinusButton",
                         "experiencePlusButton", "levelUpButton" })
                serialized.FindProperty(field).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (Button button in hud.GetComponentsInChildren<Button>(true))
            {
                if (button == null)
                    continue;
                Transform parent = button.transform.parent;
                UnityEngine.Object.DestroyImmediate(
                    parent != null && parent.name.Contains("Border")
                        ? parent.gameObject : button.gameObject);
            }
            ExperiencePickupSpawner pickupSpawner = hud.GetComponent<ExperiencePickupSpawner>();
            if (pickupSpawner != null)
                UnityEngine.Object.DestroyImmediate(pickupSpawner);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static void SaveMeleePrefabWithoutPickup()
    {
        GameObject melee = PrefabUtility.LoadPrefabContents(MeleePrefabPath);
        try
        {
            PrefabUtility.SaveAsPrefabAsset(melee, MeleePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(melee);
        }
    }

    public static void Validate()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            if (hud.GetComponentsInChildren<Button>(true).Length != 0 ||
                hud.GetComponent<ExperiencePickupSpawner>() != null)
                throw new InvalidOperationException("Debug HUD buttons remain.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
        Scene scene = EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new InvalidOperationException("Play scene is missing.");
        BasicRangedEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<BasicRangedEnemySpawner2D>();
        if (spawner == null || spawner.FirstSpawnAfterSeconds != 60f ||
            spawner.MaximumAlive != 12 ||
            new SerializedObject(spawner).FindProperty("enemyPrefab").objectReferenceValue !=
            AssetDatabase.LoadAssetAtPath<BasicRangedEnemy2D>(EnemyPrefabPath))
            throw new InvalidOperationException("Ranged spawner values or prefab reference are invalid.");
        GameObject enemy = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        try
        {
            BasicRangedEnemy2D ranged = enemy.GetComponent<BasicRangedEnemy2D>();
            if (ranged == null || ranged.MaxHealth != 20f || ranged.AttackDamage != 7f ||
                ranged.MoveSpeed != 1f || ranged.ExperienceOnDeath != 5f ||
                enemy.GetComponent<CircleCollider2D>() == null ||
                new SerializedObject(ranged).FindProperty("projectilePrefab").objectReferenceValue !=
                AssetDatabase.LoadAssetAtPath<EnemyProjectile2D>(ProjectilePrefabPath))
                throw new InvalidOperationException("Ranged enemy values or projectile are invalid.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(enemy);
        }
        EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(FindObjectsSortMode.None).Length != 4)
            throw new InvalidOperationException("Prototype_Test was not preserved.");
        Debug.Log("Ranged enemy, 60-second spawn, shared HUD, and test scene validated.");
    }
}
