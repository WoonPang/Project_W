using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TankMeleeEnemySetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestPlayScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string SpritePath = "Assets/03. Asset/Sprites/TankMeleeEnemy.png";
    private const string SquareSpritePath = "Assets/03. Asset/Sprites/BasicMeleeEnemy.png";
    private const string PrefabPath = "Assets/04. Prefabs/Enemies/TankMeleeEnemy.prefab";

    public static void Create()
    {
        Sprite pentagon = CreatePentagonSprite();
        TankMeleeEnemy2D prefab = CreatePrefab(pentagon);
        ConfigureScene(PlayScenePath, prefab);
        ConfigureScene(TestPlayScenePath, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Pentagon tank, 3-minute spawn, and both monster scenes are ready.");
    }

    private static Sprite CreatePentagonSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null)
            return existing;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2[] corners = new Vector2[5];
        for (int index = 0; index < corners.Length; index++)
        {
            float angle = Mathf.PI * 0.5f + index * Mathf.PI * 2f / corners.Length;
            corners[index] = new Vector2(size * 0.5f + Mathf.Cos(angle) * 57f,
                size * 0.5f + Mathf.Sin(angle) * 57f);
        }

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int covered = 0;
            for (int sampleY = 0; sampleY < 2; sampleY++)
            for (int sampleX = 0; sampleX < 2; sampleX++)
            {
                Vector2 sample = new Vector2(x + (sampleX + 0.5f) * 0.5f,
                    y + (sampleY + 0.5f) * 0.5f);
                if (IsInsidePentagon(sample, corners))
                    covered++;
            }
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(covered * 255 / 4));
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
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static bool IsInsidePentagon(Vector2 point, Vector2[] corners)
    {
        float firstSign = 0f;
        for (int index = 0; index < corners.Length; index++)
        {
            Vector2 start = corners[index];
            Vector2 edge = corners[(index + 1) % corners.Length] - start;
            Vector2 offset = point - start;
            float cross = edge.x * offset.y - edge.y * offset.x;
            if (Mathf.Abs(cross) <= 0.0001f)
                continue;
            float sign = Mathf.Sign(cross);
            if (firstSign == 0f)
                firstSign = sign;
            else if (sign != firstSign)
                return false;
        }
        return true;
    }

    private static TankMeleeEnemy2D CreatePrefab(Sprite sprite)
    {
        if (sprite == null)
            throw new InvalidOperationException("Tank pentagon sprite is missing.");
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (square == null)
            throw new InvalidOperationException("Health bar square sprite is missing.");

        GameObject root = new GameObject("TankMeleeEnemy");
        try
        {
            root.transform.localScale = Vector3.one * 1.25f;
            SpriteRenderer body = root.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = 9;

            GameObject outline = new GameObject("Enemy Outline", typeof(SpriteRenderer));
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = Vector3.one * 1.12f;
            SpriteRenderer outlineRenderer = outline.GetComponent<SpriteRenderer>();
            outlineRenderer.sprite = sprite;
            outlineRenderer.color = new Color(0.7f, 0.3f, 0.34f);
            outlineRenderer.sortingOrder = 8;

            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            PolygonCollider2D collider = root.AddComponent<PolygonCollider2D>();
            Vector2[] points = new Vector2[5];
            for (int index = 0; index < points.Length; index++)
            {
                float angle = Mathf.PI * 0.5f + index * Mathf.PI * 2f / points.Length;
                points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.43f;
            }
            collider.points = points;
            collider.isTrigger = true;
            root.AddComponent<EnemyStatus2D>();

            CreateHealthBarPart(root.transform, "Health Bar Background", square,
                new Color(0.08f, 0.12f, 0.15f), 1.02f, 0.14f, 0.75f, 11);
            SpriteRenderer fill = CreateHealthBarPart(root.transform, "Health Bar Fill",
                square, new Color(0.28f, 0.9f, 0.42f), 0.94f, 0.08f, 0.76f, 12);
            root.AddComponent<TankMeleeEnemy2D>().Configure(body, fill);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath)
                .GetComponent<TankMeleeEnemy2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer CreateHealthBarPart(Transform parent, string name,
        Sprite sprite, Color color, float width, float height, float y, int order)
    {
        GameObject part = new GameObject(name, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = new Vector3(0f, y, order == 11 ? -0.01f : -0.02f);
        part.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void ConfigureScene(string path, TankMeleeEnemy2D prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        if (camera == null || player == null || clock == null)
            throw new InvalidOperationException("Tank scene needs a camera, player, and game clock: " + path);
        TankMeleeEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<TankMeleeEnemySpawner2D>();
        if (spawner == null)
            spawner = new GameObject("Tank Melee Enemy Spawner")
                .AddComponent<TankMeleeEnemySpawner2D>();
        spawner.Configure(prefab, camera, player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Validate()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            TankMeleeEnemy2D tank = prefab.GetComponent<TankMeleeEnemy2D>();
            SpriteRenderer body = prefab.GetComponent<SpriteRenderer>();
            SpriteRenderer outline = prefab.transform.Find("Enemy Outline")?.GetComponent<SpriteRenderer>();
            PolygonCollider2D collider = prefab.GetComponent<PolygonCollider2D>();
            if (tank == null || tank.MaxHealth != 32.5f || tank.AttackDamage != 5f ||
                tank.MoveSpeed != 0.75f || tank.ExperienceOnDeath != 7.5f ||
                body == null || body.sprite == null || body.color != Color.white ||
                outline == null || outline.sprite != body.sprite ||
                outline.sortingOrder >= body.sortingOrder ||
                collider == null || !collider.isTrigger || collider.points.Length != 5 ||
                prefab.GetComponent<EnemyStatus2D>() == null ||
                prefab.transform.Find("Health Bar Fill") == null)
                throw new InvalidOperationException("Tank prefab does not match the PDF or pentagon design.");
            MethodInfo awake = typeof(TankMeleeEnemy2D).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(tank, null);
            tank.TakeDamage(10f);
            if (tank.CurrentHealth != 22.5f)
                throw new InvalidOperationException("Tank damage or HP bar is not connected.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        ValidateScene(PlayScenePath, false);
        ValidateScene(TestPlayScenePath, true);
        Scene test = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (!test.IsValid() ||
            UnityEngine.Object.FindFirstObjectByType<TankMeleeEnemySpawner2D>() != null)
            throw new InvalidOperationException("Prototype_Test should remain a dummy-only scene.");
        Debug.Log("Level-scaled tank HP, pentagon prefab, 3-minute spawn, and both play scenes validated.");
    }

    private static void ValidateScene(string path, bool needsTestButtons)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        TankMeleeEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<TankMeleeEnemySpawner2D>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (!scene.IsValid() || spawner == null || clock == null || hud == null ||
            spawner.FirstSpawnAfterSeconds != 180f || spawner.MaximumAlive != 8 ||
            new SerializedObject(spawner).FindProperty("enemyPrefab").objectReferenceValue !=
            AssetDatabase.LoadAssetAtPath<TankMeleeEnemy2D>(PrefabPath) ||
            (hud.transform.Find("+1min Border") != null) != needsTestButtons)
            throw new InvalidOperationException("Tank spawner or test controls are wrong in " + path);
        if (needsTestButtons)
        {
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
                typeof(PlayerStats).GetMethod("Awake", BindingFlags.Instance |
                    BindingFlags.NonPublic).Invoke(player, null);
                MethodInfo update = typeof(TankMeleeEnemySpawner2D).GetMethod("Update",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                clock.AdvanceSeconds(179f);
                update.Invoke(spawner, null);
                if (UnityEngine.Object.FindObjectsByType<TankMeleeEnemy2D>(
                        FindObjectsSortMode.None).Length != 0)
                    throw new InvalidOperationException("Tank appeared before three minutes.");
                clock.AdvanceSeconds(1f);
                update.Invoke(spawner, null);
                TankMeleeEnemy2D[] spawned = UnityEngine.Object.FindObjectsByType<TankMeleeEnemy2D>(
                    FindObjectsSortMode.None);
                if (spawner.ElapsedSeconds != 180f || spawned.Length != 1)
                    throw new InvalidOperationException("Tank did not appear at three minutes.");
                Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                Vector3 viewport = camera.WorldToViewportPoint(spawned[0].transform.position);
                if (viewport.x >= 0f && viewport.x <= 1f &&
                    viewport.y >= 0f && viewport.y <= 1f ||
                    Vector2.Distance(spawned[0].transform.position, player.transform.position) < 7f)
                    throw new InvalidOperationException("Tank spawned on-screen or beside the player.");
                UnityEngine.Object.DestroyImmediate(spawned[0].gameObject);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
