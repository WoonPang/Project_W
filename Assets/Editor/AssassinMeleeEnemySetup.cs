using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AssassinMeleeEnemySetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestPlayScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string SquareSpritePath = "Assets/03. Asset/Sprites/BasicMeleeEnemy.png";
    private const string TriangleSpritePath = "Assets/03. Asset/Sprites/AssassinMeleeEnemy.png";
    private const string PrefabPath = "Assets/04. Prefabs/Enemies/AssassinMeleeEnemy.prefab";

    public static void Create()
    {
        AssassinMeleeEnemy2D prefab = CreatePrefab(CreateTriangleSprite());
        ConfigureScene(PlayScenePath, prefab);
        ConfigureScene(TestPlayScenePath, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Fast triangle assassin and 7-minute spawners are ready.");
    }

    private static Sprite CreateTriangleSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(TriangleSpritePath);
        if (existing != null)
            return existing;
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 top = new Vector2(64f, 117f);
        Vector2 left = new Vector2(13f, 18f);
        Vector2 right = new Vector2(115f, 18f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int covered = 0;
            for (int sampleY = 0; sampleY < 2; sampleY++)
            for (int sampleX = 0; sampleX < 2; sampleX++)
            {
                Vector2 point = new Vector2(x + (sampleX + 0.5f) * 0.5f,
                    y + (sampleY + 0.5f) * 0.5f);
                if (InsideTriangle(point, top, left, right))
                    covered++;
            }
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(covered * 255 / 4));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(TriangleSpritePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TriangleSpritePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TriangleSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(TriangleSpritePath);
    }

    private static bool InsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float ab = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
        float bc = (c.x - b.x) * (point.y - b.y) - (c.y - b.y) * (point.x - b.x);
        float ca = (a.x - c.x) * (point.y - c.y) - (a.y - c.y) * (point.x - c.x);
        return ab >= 0f && bc >= 0f && ca >= 0f ||
            ab <= 0f && bc <= 0f && ca <= 0f;
    }

    private static AssassinMeleeEnemy2D CreatePrefab(Sprite triangle)
    {
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (square == null || triangle == null)
            throw new InvalidOperationException("Assassin sprites are missing.");
        GameObject root = new GameObject("AssassinMeleeEnemy");
        try
        {
            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.54f;
            collider.isTrigger = true;
            root.AddComponent<EnemyStatus2D>();

            SpriteRenderer body = CreatePart(root.transform, "Triangle Body", triangle,
                Color.white, Vector3.one, 9);
            SpriteRenderer outline = CreatePart(root.transform, "Enemy Outline", triangle,
                new Color(0.78f, 0.24f, 0.3f), Vector3.one * 1.12f, 8);

            SpriteRenderer barBack = CreatePart(root.transform, "Health Bar Background", square,
                new Color(0.08f, 0.12f, 0.15f), new Vector3(1.02f, 0.14f, 1f), 11);
            barBack.transform.localPosition = new Vector3(0f, 0.74f, -0.01f);
            SpriteRenderer barFill = CreatePart(root.transform, "Health Bar Fill", square,
                new Color(0.28f, 0.9f, 0.42f), new Vector3(0.94f, 0.08f, 1f), 12);
            barFill.transform.localPosition = new Vector3(0f, 0.75f, -0.02f);

            root.AddComponent<AssassinMeleeEnemy2D>().Configure(body, outline, barFill);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath)
                .GetComponent<AssassinMeleeEnemy2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer CreatePart(Transform parent, string name, Sprite sprite,
        Color color, Vector3 scale, int order)
    {
        GameObject part = new GameObject(name, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localScale = scale;
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void ConfigureScene(string path, AssassinMeleeEnemy2D prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        if (camera == null || player == null || clock == null)
            throw new InvalidOperationException("Assassin scene needs a camera, player, and clock: " + path);
        AssassinMeleeEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<AssassinMeleeEnemySpawner2D>();
        if (spawner == null)
            spawner = new GameObject("Assassin Melee Enemy Spawner")
                .AddComponent<AssassinMeleeEnemySpawner2D>();
        spawner.Configure(prefab, camera, player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Validate()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            AssassinMeleeEnemy2D assassin = prefab.GetComponent<AssassinMeleeEnemy2D>();
            SpriteRenderer body = prefab.transform.Find("Triangle Body")?.GetComponent<SpriteRenderer>();
            SpriteRenderer outline = prefab.transform.Find("Enemy Outline")?.GetComponent<SpriteRenderer>();
            CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
            if (assassin == null || assassin.MaxHealth != 10f ||
                assassin.AttackDamage != 10f || assassin.MoveSpeed != 1.2f ||
                assassin.ExperienceOnDeath != 10f ||
                Mathf.Abs(assassin.ChaseWorldSpeed - 10.2f) > 0.001f ||
                assassin.ContactPauseSeconds != 0.5f ||
                body == null || body.sprite == null ||
                body.sprite != AssetDatabase.LoadAssetAtPath<Sprite>(TriangleSpritePath) ||
                outline == null || outline.sprite != body.sprite ||
                outline.sortingOrder >= body.sortingOrder ||
                prefab.transform.Find("Dash Warning") != null ||
                collider == null || !collider.isTrigger ||
                prefab.GetComponent<EnemyStatus2D>() == null ||
                prefab.transform.Find("Health Bar Fill") == null)
                throw new InvalidOperationException("Fast triangle assassin prefab or PDF values are incomplete.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        ValidateScene(PlayScenePath, false);
        ValidateScene(TestPlayScenePath, true);
        ValidateContactAttack();
        Scene test = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (!test.IsValid() ||
            UnityEngine.Object.FindFirstObjectByType<AssassinMeleeEnemySpawner2D>() != null)
            throw new InvalidOperationException("Prototype_Test should keep only its test dummies.");
        Debug.Log("Level-scaled assassin HP, 7-minute spawn, contact damage, and both play scenes validated.");
    }

    private static void ValidateScene(string path, bool testButtons)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        AssassinMeleeEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<AssassinMeleeEnemySpawner2D>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (!scene.IsValid() || spawner == null || clock == null || hud == null ||
            spawner.FirstSpawnAfterSeconds != 420f || spawner.MaximumAlive != 8 ||
            new SerializedObject(spawner).FindProperty("enemyPrefab").objectReferenceValue !=
                AssetDatabase.LoadAssetAtPath<AssassinMeleeEnemy2D>(PrefabPath) ||
            (hud.transform.Find("+1min Border") != null) != testButtons)
            throw new InvalidOperationException("Assassin spawner or HUD is wrong in " + path);
        if (!testButtons)
            return;

        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            typeof(PlayerStats).GetMethod("Awake", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(player, null);
            MethodInfo update = typeof(AssassinMeleeEnemySpawner2D).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            clock.AdvanceSeconds(419f);
            update.Invoke(spawner, null);
            if (UnityEngine.Object.FindObjectsByType<AssassinMeleeEnemy2D>(
                    FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Assassin appeared before seven minutes.");
            clock.AdvanceSeconds(1f);
            update.Invoke(spawner, null);
            AssassinMeleeEnemy2D[] spawned = UnityEngine.Object.FindObjectsByType<AssassinMeleeEnemy2D>(
                FindObjectsSortMode.None);
            if (spawner.ElapsedSeconds != 420f || spawned.Length != 1)
                throw new InvalidOperationException("Assassin did not appear at seven minutes.");
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            Vector3 viewport = camera.WorldToViewportPoint(spawned[0].transform.position);
            if (viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f ||
                Vector2.Distance(spawned[0].transform.position, player.transform.position) < 7f)
                throw new InvalidOperationException("Assassin spawned on-screen or beside the player.");
            UnityEngine.Object.DestroyImmediate(spawned[0].gameObject);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private static void ValidateContactAttack()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        typeof(PlayerStats).GetMethod("Awake", BindingFlags.Instance |
            BindingFlags.NonPublic).Invoke(player, null);
        Vector3 originalPosition = player.transform.position;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        try
        {
            instance.transform.position = originalPosition + Vector3.left * 0.8f;
            AssassinMeleeEnemy2D assassin = instance.GetComponent<AssassinMeleeEnemy2D>();
            Type type = typeof(AssassinMeleeEnemy2D);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetMethod("Awake", flags).Invoke(assassin, null);
            type.GetMethod("Update", flags).Invoke(assassin, null);
            if (player.CurrentHealth != 90f || !assassin.IsContactPaused)
                throw new InvalidOperationException("Assassin should deal 10 contact damage without a windup.");
            type.GetMethod("Update", flags).Invoke(assassin, null);
            if (player.CurrentHealth != 90f)
                throw new InvalidOperationException("Assassin attacked again before its interval.");
        }
        finally
        {
            player.transform.position = originalPosition;
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
