using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatPrototypeSetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PlayerPrefabPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string ProjectilePrefabPath = "Assets/04. Prefabs/Projectiles/AutoAttackProjectile.prefab";
    private const string DummyPrefabPath = "Assets/04. Prefabs/Targets/TrainingDummy.prefab";
    private const string SpriteFolder = "Assets/03. Asset/Sprites";

    // One-time setup for the combat test in the Prototype scene.
    public static void Apply()
    {
        if (AssetDatabase.LoadMainAssetAtPath(DummyPrefabPath) != null)
            throw new InvalidOperationException("The combat prototype has already been created.");

        EnsureFolder("Assets/04. Prefabs/Projectiles");
        EnsureFolder("Assets/04. Prefabs/Targets");

        Sprite dummySprite = CreateSprite(SpriteFolder + "/TrainingDummy.png", DrawDummy);
        Sprite projectileSprite = CreateSprite(SpriteFolder + "/AutoAttackProjectile.png", DrawProjectile);
        Sprite whiteSprite = CreateSprite(SpriteFolder + "/WhitePixel.png", (x, y) => Color.white);

        Projectile2D projectilePrefab = CreateProjectilePrefab(projectileSprite);
        GameObject dummyPrefab = CreateDummyPrefab(dummySprite, whiteSprite);
        ConfigurePlayerPrefab(projectilePrefab);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Landmark")
                UnityEngine.Object.DestroyImmediate(root);
        }

        GameObject dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, scene);
        dummy.transform.position = new Vector3(3f, 0f, 0f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        if (!AssetDatabase.DeleteAsset(SpriteFolder + "/Landmark.png"))
            Debug.LogWarning("Landmark.png was not found; scene objects were removed.");

        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Combat prototype setup completed.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static Sprite CreateSprite(string path, Func<int, int, Color> draw)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, draw(x, y));
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Sprite import failed: " + path);
        return sprite;
    }

    private static Projectile2D CreateProjectilePrefab(Sprite sprite)
    {
        GameObject projectile = new GameObject("Auto Attack Projectile");
        projectile.transform.localScale = Vector3.one * 0.28f;
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 12;
        projectile.AddComponent<Projectile2D>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath);
        UnityEngine.Object.DestroyImmediate(projectile);
        if (prefab == null)
            throw new InvalidOperationException("Projectile prefab could not be saved.");
        return prefab.GetComponent<Projectile2D>();
    }

    private static GameObject CreateDummyPrefab(Sprite dummySprite, Sprite whiteSprite)
    {
        GameObject dummy = new GameObject("Training Dummy");
        SpriteRenderer body = dummy.AddComponent<SpriteRenderer>();
        body.sprite = dummySprite;
        body.sortingOrder = 5;
        BoxCollider2D collider = dummy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 0.8f);
        TrainingDummy2D target = dummy.AddComponent<TrainingDummy2D>();

        GameObject barBackground = new GameObject("Health Bar Background");
        barBackground.transform.SetParent(dummy.transform, false);
        barBackground.transform.localPosition = new Vector3(0f, 0.78f, -0.01f);
        barBackground.transform.localScale = new Vector3(1.4f, 0.18f, 1f);
        SpriteRenderer backgroundRenderer = barBackground.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = whiteSprite;
        backgroundRenderer.color = new Color(0.08f, 0.12f, 0.15f);
        backgroundRenderer.sortingOrder = 19;

        GameObject barFill = new GameObject("Health Bar Fill");
        barFill.transform.SetParent(dummy.transform, false);
        barFill.transform.localPosition = new Vector3(0f, 0.78f, -0.02f);
        barFill.transform.localScale = new Vector3(1.3f, 0.11f, 1f);
        SpriteRenderer fillRenderer = barFill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = whiteSprite;
        fillRenderer.color = new Color(0.28f, 0.9f, 0.42f);
        fillRenderer.sortingOrder = 20;

        target.Configure(body, fillRenderer);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(dummy, DummyPrefabPath);
        UnityEngine.Object.DestroyImmediate(dummy);
        if (prefab == null)
            throw new InvalidOperationException("Training dummy prefab could not be saved.");
        return prefab;
    }

    private static void ConfigurePlayerPrefab(Projectile2D projectilePrefab)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (player.GetComponent<PlayerStats>() == null)
                player.AddComponent<PlayerStats>();

            AutoAttack2D attack = player.GetComponent<AutoAttack2D>();
            if (attack == null)
                attack = player.AddComponent<AutoAttack2D>();
            attack.SetProjectilePrefab(projectilePrefab);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static Color DrawDummy(int x, int y)
    {
        if (x < 7 || x > 56 || y < 7 || y > 56)
            return Color.clear;
        if (x < 11 || x > 52 || y < 11 || y > 52)
            return new Color(0.32f, 0.19f, 0.12f);
        if (Mathf.Abs(x - y) < 3 || Mathf.Abs(x + y - 63) < 3)
            return new Color(1f, 0.84f, 0.55f);
        return new Color(0.68f, 0.41f, 0.23f);
    }

    private static Color DrawProjectile(int x, int y)
    {
        float dx = x - 31.5f;
        float dy = y - 31.5f;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        if (distance > 28f)
            return Color.clear;
        if (distance > 23f)
            return new Color(1f, 0.55f, 0.15f, Mathf.Clamp01(28f - distance));
        return new Color(1f, 0.94f, 0.42f);
    }

    private static void Validate()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == "Landmark")
                throw new InvalidOperationException("A Landmark remains in the Prototype scene.");

        PlayerStats stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        AutoAttack2D attack = UnityEngine.Object.FindFirstObjectByType<AutoAttack2D>();
        TrainingDummy2D dummy = UnityEngine.Object.FindFirstObjectByType<TrainingDummy2D>();
        if (stats == null || attack == null || dummy == null)
            throw new InvalidOperationException("Player stats, attack, or dummy is missing.");
        if (stats.MaxHealth != 100f || stats.AttackDamage != 10f ||
            stats.MoveSpeed != 1f || stats.AttackSpeed != 1f)
            throw new InvalidOperationException("Player base stats do not match the prototype spec.");
        if (new SerializedObject(attack).FindProperty("projectilePrefab").objectReferenceValue == null)
            throw new InvalidOperationException("Auto attack projectile is not assigned.");

        Debug.Log("Combat prototype validation passed.");
    }
}
