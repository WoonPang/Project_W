using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BarrageSkillSetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PlayerPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string DummyPath = "Assets/04. Prefabs/Targets/TrainingDummy.prefab";
    private const string BarragePath = "Assets/04. Prefabs/Projectiles/BarrageProjectile.prefab";
    private const string BulletSpritePath = "Assets/03. Asset/Sprites/BarrageBullet.png";

    public static void Apply()
    {
        Sprite sprite = CreateBulletSprite();
        if (sprite == null)
            throw new InvalidOperationException("Projectile sprite is missing.");
        Projectile2D barragePrefab = CreateBarrageProjectile(sprite);
        ConfigurePlayer(barragePrefab);
        ConfigureDummy();
        ArrangeDummies();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Indiscriminate Barrage and four 200 HP test dummies are ready.");
    }

    private static Projectile2D CreateBarrageProjectile(Sprite sprite)
    {
        GameObject root = new GameObject("Barrage Projectile");
        root.transform.localScale = Vector3.one * 0.22f;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 13;
        root.AddComponent<Projectile2D>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BarragePath);
        UnityEngine.Object.DestroyImmediate(root);
        if (prefab == null)
            throw new InvalidOperationException("Barrage projectile prefab could not be saved.");
        return prefab.GetComponent<Projectile2D>();
    }

    private static Sprite CreateBulletSprite()
    {
        string absolutePath = Path.Combine(Application.dataPath, "03. Asset/Sprites/BarrageBullet.png");
        if (!File.Exists(absolutePath))
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            try
            {
                Color32[] pixels = new Color32[64 * 64];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                        if (distance > 28f)
                            pixels[y * 64 + x] = new Color32(0, 0, 0, 0);
                        else if (distance > 23f)
                            pixels[y * 64 + x] = new Color32(63, 218, 255,
                                (byte)Mathf.RoundToInt((28f - distance) / 5f * 170f));
                        else if (distance > 12f)
                            pixels[y * 64 + x] = new Color32(78, 226, 255, 255);
                        else
                            pixels[y * 64 + x] = new Color32(221, 251, 255, 255);
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        AssetDatabase.ImportAsset(BulletSpritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BulletSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BulletSpritePath);
    }

    private static void ConfigurePlayer(Projectile2D barragePrefab)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            RSkillCooldown skill = player.GetComponent<RSkillCooldown>();
            if (skill == null)
                throw new InvalidOperationException("Player R skill component is missing.");
            skill.ConfigureProjectile(barragePrefab);
            SerializedObject serialized = new SerializedObject(skill);
            serialized.FindProperty("damageMultiplier").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static void ConfigureDummy()
    {
        GameObject dummy = PrefabUtility.LoadPrefabContents(DummyPath);
        try
        {
            TrainingDummy2D target = dummy.GetComponent<TrainingDummy2D>();
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty("maxHealth").floatValue = 200f;
            serialized.FindProperty("currentHealth").floatValue = 200f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(dummy, DummyPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(dummy);
        }
    }

    private static void ArrangeDummies()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (TrainingDummy2D existing in UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
                     FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPath);
        AddDummy(scene, dummyPrefab, "Training Dummy North 2.4", new Vector3(0f, 2.4f, 0f));
        AddDummy(scene, dummyPrefab, "Training Dummy East 4.1", new Vector3(4.1f, 0f, 0f));
        AddDummy(scene, dummyPrefab, "Training Dummy South 5.1", new Vector3(0f, -5.1f, 0f));
        AddDummy(scene, dummyPrefab, "Training Dummy West 7.2", new Vector3(-7.2f, 0f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void AddDummy(UnityEngine.SceneManagement.Scene scene,
        GameObject prefab, string name, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = name;
        instance.transform.position = position;
    }

    private static void Validate()
    {
        RSkillCooldown skill = UnityEngine.Object.FindFirstObjectByType<RSkillCooldown>();
        Camera camera = Camera.main;
        if (skill == null || camera == null)
            throw new InvalidOperationException("Player R skill or camera is missing.");
        SerializedObject serializedSkill = new SerializedObject(skill);
        if (serializedSkill.FindProperty("projectilePrefab").objectReferenceValue == null ||
            serializedSkill.FindProperty("totalShots").intValue != 50 ||
            serializedSkill.FindProperty("shotsPerSecond").floatValue != 10f ||
            serializedSkill.FindProperty("damageMultiplier").floatValue != 1.5f ||
            skill.DamagePerShot != 15f ||
            skill.CooldownSeconds != 30f || skill.SkillName != "무차별 난사")
            throw new InvalidOperationException("Barrage settings are incorrect.");
        Projectile2D barrage = (Projectile2D)serializedSkill.FindProperty("projectilePrefab").objectReferenceValue;
        if (barrage.GetComponent<SpriteRenderer>().sprite == null ||
            barrage.GetComponent<SpriteRenderer>().sprite.name != "BarrageBullet")
            throw new InvalidOperationException("Barrage projectile artwork is missing.");

        TrainingDummy2D[] dummies = UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
            FindObjectsSortMode.None);
        if (dummies.Length != 4)
            throw new InvalidOperationException("Expected four test dummies.");
        foreach (TrainingDummy2D dummy in dummies)
        {
            if (dummy.MaxHealth != 200f)
                throw new InvalidOperationException("A dummy does not have 200 HP.");
            Vector3 viewport = camera.WorldToViewportPoint(dummy.transform.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
                throw new InvalidOperationException("A dummy is outside the starting camera view.");
        }
        if (skill.FindNearestVisibleDummy() == null ||
            skill.FindNearestVisibleDummy().name != "Training Dummy North 2.4")
            throw new InvalidOperationException("R skill did not select the nearest visible dummy.");

        GameObject test = new GameObject("R Damage Multiplier Validation");
        try
        {
            PlayerStats testStats = test.AddComponent<PlayerStats>();
            RSkillCooldown testSkill = test.AddComponent<RSkillCooldown>();
            testStats.AddAttackDamage(5f);
            if (testSkill.DamagePerShot != 22.5f)
                throw new InvalidOperationException("R damage did not follow increased attack damage.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(test);
        }
        Debug.Log("Barrage target, four dummy positions, and skill settings validated.");
    }
}
