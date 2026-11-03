using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChargingRangedEnemySetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestPlayScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string SpritePath = "Assets/03. Asset/Sprites/ChargingRangedEnemy.png";
    private const string SquareSpritePath = "Assets/03. Asset/Sprites/BasicMeleeEnemy.png";
    private const string PrefabPath = "Assets/04. Prefabs/Enemies/ChargingRangedEnemy.prefab";

    public static void Create()
    {
        Sprite sprite = CreateDoubleCircleSprite();
        ChargingRangedEnemy2D prefab = CreatePrefab(sprite);
        ConfigureScene(PlayScenePath, prefab);
        ConfigureScene(TestPlayScenePath, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Double-circle charging ranged enemy and 9-minute spawners are ready.");
    }

    private static Sprite CreateDoubleCircleSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null)
            return existing;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int covered = 0;
            for (int sampleY = 0; sampleY < 2; sampleY++)
            for (int sampleX = 0; sampleX < 2; sampleX++)
            {
                float dx = x + (sampleX + 0.5f) * 0.5f - size * 0.5f;
                float dy = y + (sampleY + 0.5f) * 0.5f - size * 0.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                if (radius <= 23f || radius >= 43f && radius <= 59f)
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

    private static ChargingRangedEnemy2D CreatePrefab(Sprite sprite)
    {
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (sprite == null || square == null)
            throw new InvalidOperationException("Charging enemy sprites are missing.");
        GameObject root = new GameObject("ChargingRangedEnemy");
        try
        {
            SpriteRenderer body = root.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.sortingOrder = 9;
            body.color = Color.white;

            GameObject outline = new GameObject("Enemy Outline", typeof(SpriteRenderer));
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = Vector3.one * 1.1f;
            SpriteRenderer outlineRenderer = outline.GetComponent<SpriteRenderer>();
            outlineRenderer.sprite = sprite;
            outlineRenderer.color = new Color(0.7f, 0.3f, 0.34f);
            outlineRenderer.sortingOrder = 8;

            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.52f;
            collider.isTrigger = true;
            root.AddComponent<EnemyStatus2D>();

            CreateBar(root.transform, "Health Bar Background", square,
                new Color(0.08f, 0.12f, 0.15f), 1.02f, 0.14f, 0.77f, 11);
            SpriteRenderer healthFill = CreateBar(root.transform, "Health Bar Fill", square,
                new Color(0.28f, 0.9f, 0.42f), 0.94f, 0.08f, 0.78f, 12);
            SpriteRenderer glow = CreateBeam(root.transform, "Laser Warning Glow", square, 14);
            SpriteRenderer core = CreateBeam(root.transform, "Laser Warning Core", square, 15);
            SpriteRenderer flash = CreateBeam(root.transform, "Laser Flash", square, 16);
            root.AddComponent<ChargingRangedEnemy2D>().Configure(
                body, healthFill, glow, core, flash);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath)
                .GetComponent<ChargingRangedEnemy2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer CreateBar(Transform parent, string name, Sprite sprite,
        Color color, float width, float height, float y, int order)
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

    private static SpriteRenderer CreateBeam(Transform parent, string name,
        Sprite sprite, int order)
    {
        GameObject part = new GameObject(name, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = new Vector3(0f, 0f, -0.03f);
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.1f, 0.1f, 0f);
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    private static void ConfigureScene(string path, ChargingRangedEnemy2D prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        if (camera == null || player == null || clock == null)
            throw new InvalidOperationException("Charging enemy scene needs a camera, player, and clock: " + path);
        ChargingRangedEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<ChargingRangedEnemySpawner2D>();
        if (spawner == null)
            spawner = new GameObject("Charging Ranged Enemy Spawner")
                .AddComponent<ChargingRangedEnemySpawner2D>();
        spawner.Configure(prefab, camera, player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Validate()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ChargingRangedEnemy2D enemy = prefab.GetComponent<ChargingRangedEnemy2D>();
            SpriteRenderer body = prefab.GetComponent<SpriteRenderer>();
            SpriteRenderer outline = prefab.transform.Find("Enemy Outline")?.GetComponent<SpriteRenderer>();
            CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
            if (enemy == null || enemy.MaxHealth != 10f || enemy.AttackDamage != 15f ||
                enemy.MoveSpeed != 1f || enemy.ExperienceOnDeath != 10f ||
                enemy.ChargeSeconds != 1.8f || body == null || body.sprite == null ||
                outline == null || outline.sprite != body.sprite ||
                outline.sortingOrder >= body.sortingOrder ||
                collider == null || !collider.isTrigger ||
                prefab.transform.Find("Health Bar Fill") == null ||
                prefab.transform.Find("Laser Warning Glow")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Laser Warning Core")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Laser Flash")?.GetComponent<SpriteRenderer>() == null)
                throw new InvalidOperationException("Charging ranged prefab is incomplete or has wrong PDF values.");
            if (!ChargingRangedEnemy2D.IsPointInLaser(Vector2.zero, Vector2.right,
                    new Vector2(4f, 0.4f), 12f, 0.55f) ||
                ChargingRangedEnemy2D.IsPointInLaser(Vector2.zero, Vector2.right,
                    new Vector2(4f, 1f), 12f, 0.55f))
                throw new InvalidOperationException("Laser hit area does not match its warning line.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        ValidateScene(PlayScenePath, false);
        ValidateScene(TestPlayScenePath, true);
        ValidateLaserAttack();
        Scene test = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (!test.IsValid() ||
            UnityEngine.Object.FindFirstObjectByType<ChargingRangedEnemySpawner2D>() != null)
            throw new InvalidOperationException("Prototype_Test should keep only its test dummies.");
        Debug.Log("Level-scaled charging enemy HP, 1.8-second screen-edge laser, 9-minute spawn, and both play scenes validated.");
    }

    private static void ValidateLaserAttack()
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
            instance.transform.position = originalPosition + Vector3.right * 4f;
            ChargingRangedEnemy2D enemy = instance.GetComponent<ChargingRangedEnemy2D>();
            Type type = typeof(ChargingRangedEnemy2D);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetMethod("Awake", flags).Invoke(enemy, null);
            MethodInfo begin = type.GetMethod("BeginCharge", flags);
            MethodInfo update = type.GetMethod("UpdateCharge", flags);
            FieldInfo started = type.GetField("chargeStartedAt", flags);
            SpriteRenderer warning = instance.transform.Find("Laser Warning Core")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer warningGlow = instance.transform.Find("Laser Warning Glow")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer flash = instance.transform.Find("Laser Flash")
                .GetComponent<SpriteRenderer>();
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();

            begin.Invoke(enemy, null);
            float expectedLength = ChargingRangedEnemy2D.DistanceToCameraEdge(
                camera, instance.transform.position, Vector2.left);
            if (!warningGlow.enabled ||
                Mathf.Abs(warningGlow.transform.localScale.x - expectedLength) > 0.01f ||
                warning.transform.localScale.x >= expectedLength)
                throw new InvalidOperationException("Charging warning does not start at the screen edge.");
            Vector3 end = camera.WorldToViewportPoint(instance.transform.position +
                Vector3.left * warningGlow.transform.localScale.x);
            if (end.x > 0f || end.x < -0.02f)
                throw new InvalidOperationException("Charging warning misses the screen edge.");
            float oppositeLength = ChargingRangedEnemy2D.DistanceToCameraEdge(
                camera, instance.transform.position, Vector2.right);
            if (Mathf.Abs(oppositeLength - expectedLength) < 1f)
                throw new InvalidOperationException("Charging warning length does not change with direction.");
            started.SetValue(enemy, Time.time - 1.4f);
            update.Invoke(enemy, null);
            if (!enemy.IsCharging || !warning.enabled || player.CurrentHealth != 100f)
                throw new InvalidOperationException("Laser did not show a non-damaging charging warning.");

            player.transform.position = originalPosition + Vector3.up * 1.2f;
            started.SetValue(enemy, Time.time - 2.1f);
            update.Invoke(enemy, null);
            if (enemy.IsCharging || warning.enabled || !flash.enabled ||
                player.CurrentHealth != 100f)
                throw new InvalidOperationException("Moving off the locked laser line did not avoid damage.");

            player.transform.position = originalPosition;
            begin.Invoke(enemy, null);
            started.SetValue(enemy, Time.time - 2.1f);
            update.Invoke(enemy, null);
            if (player.CurrentHealth != 85f)
                throw new InvalidOperationException("The completed laser did not deal 15 damage.");
        }
        finally
        {
            player.transform.position = originalPosition;
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ValidateScene(string path, bool testButtons)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        ChargingRangedEnemySpawner2D spawner =
            UnityEngine.Object.FindFirstObjectByType<ChargingRangedEnemySpawner2D>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (!scene.IsValid() || spawner == null || clock == null || hud == null ||
            spawner.FirstSpawnAfterSeconds != 540f ||
            new SerializedObject(spawner).FindProperty("enemyPrefab").objectReferenceValue !=
            AssetDatabase.LoadAssetAtPath<ChargingRangedEnemy2D>(PrefabPath) ||
            (hud.transform.Find("+1min Border") != null) != testButtons)
            throw new InvalidOperationException("Charging ranged spawner or HUD is wrong in " + path);
        if (!testButtons)
            return;

        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            typeof(PlayerStats).GetMethod("Awake", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(player, null);
            MethodInfo update = typeof(ChargingRangedEnemySpawner2D).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            clock.AdvanceSeconds(539f);
            update.Invoke(spawner, null);
            if (UnityEngine.Object.FindObjectsByType<ChargingRangedEnemy2D>(
                    FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Charging enemy appeared before nine minutes.");
            clock.AdvanceSeconds(1f);
            update.Invoke(spawner, null);
            ChargingRangedEnemy2D[] spawned = UnityEngine.Object.FindObjectsByType<ChargingRangedEnemy2D>(
                FindObjectsSortMode.None);
            if (spawner.ElapsedSeconds != 540f || spawned.Length != 1)
                throw new InvalidOperationException("Charging enemy did not appear at nine minutes.");
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            Vector3 viewport = camera.WorldToViewportPoint(spawned[0].transform.position);
            if (viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f ||
                Vector2.Distance(spawned[0].transform.position, player.transform.position) < 7f)
                throw new InvalidOperationException("Charging enemy spawned on-screen or beside the player.");
            UnityEngine.Object.DestroyImmediate(spawned[0].gameObject);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }
    }
}
