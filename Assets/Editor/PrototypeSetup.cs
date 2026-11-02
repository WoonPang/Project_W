using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PrototypeSetup
{
    private const string ScenePath = "Assets/01. Scenes/MovementPrototype.unity";
    private const string PlayerPrefabPath = "Assets/04. Prefabs/Characters/Player.prefab";
    private const string PrototypeScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PrototypePlayerPrefabPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";

    // Run once with Unity's -executeMethod PrototypeSetup.CreateProject.
    public static void CreateProject()
    {
        try
        {
            EnsureFolder("Assets/03. Asset");
            EnsureFolder("Assets/03. Asset/Sprites");
            EnsureFolder("Assets/Input");
            EnsureFolder("Assets/04. Prefabs");
            EnsureFolder("Assets/04. Prefabs/Characters");
            EnsureFolder("Assets/02. Scripts");
            EnsureFolder("Assets/02. Scripts/Player");
            EnsureFolder("Assets/02. Scripts/Camera");
            EnsureFolder("Assets/02. Scripts/World");
            EnsureFolder("Assets/01. Scenes");
            EnsureFolder("Assets/Settings");

            MoveIfPresent("Assets/InputSystem_Actions.inputactions", "Assets/Input/InputSystem_Actions.inputactions");
            MoveIfPresent("Assets/DefaultVolumeProfile.asset", "Assets/Settings/DefaultVolumeProfile.asset");
            MoveIfPresent("Assets/UniversalRenderPipelineGlobalSettings.asset", "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset");

            Sprite playerSprite = CreateSprite("Assets/03. Asset/Sprites/PlayerPlaceholder.png", DrawPlayer);
            Sprite groundSprite = CreateSprite("Assets/03. Asset/Sprites/GridTile.png", DrawGrid);

            GameObject playerSource = new GameObject("Player");
            SpriteRenderer playerRenderer = playerSource.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = playerSprite;
            playerRenderer.sortingOrder = 10;
            Rigidbody2D body = playerSource.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D collider = playerSource.AddComponent<CircleCollider2D>();
            collider.radius = 0.38f;
            playerSource.AddComponent<PlayerMovement2D>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerSource, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(playerSource);
            if (prefab == null)
                throw new InvalidOperationException("Player prefab could not be saved.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject ground = new GameObject("Grid Ground");
            SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = groundSprite;
            groundRenderer.drawMode = SpriteDrawMode.Tiled;
            groundRenderer.size = new Vector2(100f, 100f);
            groundRenderer.sortingOrder = -10;

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.position = Vector3.zero;
            ground.AddComponent<TiledGroundFollow2D>().SetTarget(player.transform);

            GameObject lightObject = new GameObject("Global Light 2D");
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.12f, 0.16f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<CameraFollow2D>().SetTarget(player.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/01. Scenes/SampleScene.unity", false)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Project W movement prototype created: " + ScenePath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    // Creates a separate scene and player prefab, preserving the original movement demo.
    public static void CreatePrototypeScene()
    {
        if (AssetDatabase.LoadMainAssetAtPath(PrototypeScenePath) != null ||
            AssetDatabase.LoadMainAssetAtPath(PrototypePlayerPrefabPath) != null)
            throw new InvalidOperationException("Prototype scene or player prefab already exists.");

        Sprite playerSprite = CreateSprite(
            "Assets/03. Asset/Sprites/PrototypePlayerCircle.png", DrawPrototypePlayer, FilterMode.Bilinear);

        if (!AssetDatabase.CopyAsset(PlayerPrefabPath, PrototypePlayerPrefabPath))
            throw new InvalidOperationException("Could not copy the player prefab.");

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrototypePlayerPrefabPath);
        try
        {
            prefabContents.GetComponent<SpriteRenderer>().sprite = playerSprite;
            PrefabUtility.SaveAsPrefabAsset(prefabContents, PrototypePlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        if (!AssetDatabase.CopyAsset(ScenePath, PrototypeScenePath))
            throw new InvalidOperationException("Could not copy the movement scene.");

        Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
        PlayerMovement2D oldPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerMovement2D>();
        CameraFollow2D cameraFollow = UnityEngine.Object.FindFirstObjectByType<CameraFollow2D>();
        TiledGroundFollow2D groundFollow = UnityEngine.Object.FindFirstObjectByType<TiledGroundFollow2D>();
        GameObject prototypePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePlayerPrefabPath);

        if (oldPlayer == null || cameraFollow == null || groundFollow == null || prototypePrefab == null)
            throw new InvalidOperationException("The movement scene is missing a required player or follower.");

        GameObject newPlayer = (GameObject)PrefabUtility.InstantiatePrefab(prototypePrefab, scene);
        newPlayer.transform.SetPositionAndRotation(oldPlayer.transform.position, oldPlayer.transform.rotation);
        newPlayer.transform.localScale = oldPlayer.transform.localScale;
        cameraFollow.SetTarget(newPlayer.transform);
        groundFollow.SetTarget(newPlayer.transform);
        UnityEngine.Object.DestroyImmediate(oldPlayer.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, PrototypeScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(PrototypeScenePath, true),
            new EditorBuildSettingsScene(ScenePath, false),
            new EditorBuildSettingsScene("Assets/01. Scenes/SampleScene.unity", false)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Prototype scene and circle player created: " + PrototypeScenePath);
    }

    public static void ValidatePrototypeScene()
    {
        EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
        PlayerMovement2D player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement2D>();
        CameraFollow2D cameraFollow = UnityEngine.Object.FindFirstObjectByType<CameraFollow2D>();
        TiledGroundFollow2D groundFollow = UnityEngine.Object.FindFirstObjectByType<TiledGroundFollow2D>();
        Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/03. Asset/Sprites/PrototypePlayerCircle.png");

        if (player == null || cameraFollow == null || groundFollow == null || expectedSprite == null)
            throw new InvalidOperationException("Prototype scene is missing a required asset or component.");
        if (player.GetComponent<SpriteRenderer>().sprite != expectedSprite)
            throw new InvalidOperationException("Prototype player does not use the circle sprite.");
        if (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(player.gameObject))
            != PrototypePlayerPrefabPath)
            throw new InvalidOperationException("Prototype scene uses the wrong player prefab.");
        if (new SerializedObject(cameraFollow).FindProperty("target").objectReferenceValue != player.transform ||
            new SerializedObject(groundFollow).FindProperty("target").objectReferenceValue != player.transform)
            throw new InvalidOperationException("Camera or ground is not following the prototype player.");
        if (EditorBuildSettings.scenes.Length == 0 ||
            EditorBuildSettings.scenes[0].path != PrototypeScenePath ||
            !EditorBuildSettings.scenes[0].enabled)
            throw new InvalidOperationException("Prototype scene is not the active build scene.");

        Debug.Log("Prototype scene validation passed.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void MoveIfPresent(string source, string destination)
    {
        if (AssetDatabase.LoadMainAssetAtPath(source) == null)
            return;

        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);
    }

    private static Sprite CreateSprite(string path, Func<int, int, Color32> pixelColor,
        FilterMode filterMode = FilterMode.Point)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = filterMode;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, pixelColor(x, y));
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = filterMode;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Sprite import failed: " + path);
        return sprite;
    }

    private static Color32 DrawGrid(int x, int y)
    {
        if (x < 2 || y < 2)
            return new Color32(52, 70, 79, 255);
        return new Color32(35, 50, 59, 255);
    }

    private static Color32 DrawPlayer(int x, int y)
    {
        float dx = x - 31.5f;
        float dy = y - 31.5f;
        float radiusSquared = dx * dx + dy * dy;
        if (radiusSquared > 29f * 29f)
            return new Color32(0, 0, 0, 0);
        if (radiusSquared > 25f * 25f)
            return new Color32(13, 31, 43, 255);
        if (y > 34 && y < 56 && Mathf.Abs(dx) < (y - 30f) * 0.42f)
            return new Color32(250, 251, 242, 255);
        return new Color32(67, 209, 222, 255);
    }

    private static Color32 DrawPrototypePlayer(int x, int y)
    {
        float dx = x - 31.5f;
        float dy = y - 31.5f;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        float alpha = Mathf.Clamp01(29f - distance);
        float border = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(23f, 24f, distance));
        Color purple = new Color(0.53f, 0.28f, 0.79f, 1f);
        Color skyBlue = new Color(0.40f, 0.88f, 0.98f, 1f);
        Color color = Color.Lerp(purple, skyBlue, border);
        color.a = alpha;
        return color;
    }

}
