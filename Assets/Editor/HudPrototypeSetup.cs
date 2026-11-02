using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HudPrototypeSetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PlayerPrefabPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string HudPrefabPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string WhiteSpritePath = "Assets/03. Asset/Sprites/WhitePixel.png";
    private const string CircleSpritePath = "Assets/03. Asset/Sprites/HudCircle.png";

    public static void Apply()
    {
        if (AssetDatabase.LoadMainAssetAtPath(HudPrefabPath) != null)
            throw new InvalidOperationException("Player HUD already exists.");

        if (!AssetDatabase.IsValidFolder("Assets/04. Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/04. Prefabs", "UI");

        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        if (white == null)
            throw new InvalidOperationException("WhitePixel sprite is missing.");
        Sprite circle = CreateCircleSprite();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            throw new InvalidOperationException("Unity's built-in UI font is missing.");

        ConfigurePlayer();
        GameObject hudPrefab = CreateHudPrefab(white, circle, font);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PrefabUtility.InstantiatePrefab(hudPrefab, scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Player HUD and R cooldown created.");
    }

    private static void ConfigurePlayer()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (player.GetComponent<RSkillCooldown>() == null)
                player.AddComponent<RSkillCooldown>();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static GameObject CreateHudPrefab(Sprite white, Sprite circle, Font font)
    {
        GameObject root = new GameObject("Player HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerHudUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Color black = new Color(0.03f, 0.03f, 0.03f);
        Color whiteColor = Color.white;

        CreateImage(root.transform, "HP Border", white, black, 95f, 25f, 647f, 50f);
        Image hpFill = CreateImage(root.transform, "HP Fill", white,
            new Color(0.94f, 0.07f, 0.08f), 97f, 27f, 643f, 46f);
        SetHorizontalFill(hpFill);
        Text hpText = CreateText(root.transform, "HP Text", font, "HP : 100/100",
            32, FontStyle.Bold, whiteColor, TextAnchor.MiddleLeft, 309f, 27f, 360f, 46f);

        CreateImage(root.transform, "EXP Border", white, black, 174f, 74f, 517f, 50f);
        Image expFill = CreateImage(root.transform, "EXP Fill", white,
            new Color(0.51f, 0.53f, 0.91f), 176f, 76f, 513f, 46f);
        SetHorizontalFill(expFill);
        expFill.fillAmount = 0f;
        Text expText = CreateText(root.transform, "EXP Text", font, "EXP 0%",
            32, FontStyle.Bold, whiteColor, TextAnchor.MiddleLeft, 309f, 76f, 340f, 46f);

        CreateImage(root.transform, "Stats Background", white,
            new Color(0.88f, 0.88f, 0.88f), 175f, 125f, 350f, 50f);
        Text attackText = CreateText(root.transform, "Attack Stat", font, "공격력 : 10",
            16, FontStyle.Normal, black, TextAnchor.MiddleLeft, 194f, 132f, 103f, 36f);
        Text attackSpeedText = CreateText(root.transform, "Attack Speed Stat", font, "공격속도 : 1",
            16, FontStyle.Normal, black, TextAnchor.MiddleLeft, 296f, 132f, 117f, 36f);
        Text moveSpeedText = CreateText(root.transform, "Move Speed Stat", font, "이동속도 : 1",
            16, FontStyle.Normal, black, TextAnchor.MiddleLeft, 410f, 132f, 112f, 36f);

        CreateImage(root.transform, "R Slot Border", white, black, 25f, 185f, 100f, 100f);
        CreateImage(root.transform, "R Slot", white,
            new Color(0.85f, 0.85f, 0.85f), 27f, 187f, 96f, 96f);
        Image qOverlay = CreateImage(root.transform, "R Cooldown Overlay", white,
            new Color(0f, 0f, 0f, 0.65f), 27f, 187f, 96f, 96f);
        qOverlay.type = Image.Type.Filled;
        qOverlay.fillMethod = Image.FillMethod.Radial360;
        qOverlay.fillOrigin = (int)Image.Origin360.Top;
        qOverlay.fillClockwise = false;
        qOverlay.enabled = false;
        Text countdownText = CreateText(root.transform, "R Countdown", font, "",
            31, FontStyle.Bold, whiteColor, TextAnchor.MiddleCenter, 37f, 211f, 76f, 50f);
        countdownText.enabled = false;
        CreateText(root.transform, "R Key Label", font, "R",
            29, FontStyle.Normal, black, TextAnchor.LowerRight, 95f, 246f, 28f, 37f);

        CreateImage(root.transform, "Portrait Border", circle, black, 25f, 25f, 150f, 150f);
        CreateImage(root.transform, "Portrait Placeholder", circle, whiteColor, 29f, 29f, 142f, 142f);

        root.GetComponent<PlayerHudUI>().Configure(hpFill, expFill, qOverlay,
            hpText, expText, attackText, attackSpeedText, moveSpeedText, countdownText);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (prefab == null)
            throw new InvalidOperationException("Player HUD prefab could not be saved.");
        return prefab;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
        float x, float y, float width, float height)
    {
        GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        element.transform.SetParent(parent, false);
        SetRect(element.GetComponent<RectTransform>(), x, y, width, height);
        Image image = element.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Font font, string value,
        int fontSize, FontStyle style, Color color, TextAnchor alignment,
        float x, float y, float width, float height)
    {
        GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        element.transform.SetParent(parent, false);
        SetRect(element.GetComponent<RectTransform>(), x, y, width, height);
        Text label = element.GetComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetHorizontalFill(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 63.5f;
                float dy = y - 63.5f;
                float alpha = Mathf.Clamp01(62.5f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        File.WriteAllBytes(CircleSpritePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(CircleSpritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(CircleSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        if (sprite == null)
            throw new InvalidOperationException("HUD circle sprite could not be imported.");
        return sprite;
    }

    private static void Validate()
    {
        PlayerStats stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        RSkillCooldown rSkill = UnityEngine.Object.FindFirstObjectByType<RSkillCooldown>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (stats == null || rSkill == null || hud == null)
            throw new InvalidOperationException("Player or HUD components are missing from Prototype.");
        if (rSkill.CooldownSeconds != 30f)
            throw new InvalidOperationException("R cooldown must be 30 seconds.");
        if (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(hud.gameObject))
            != HudPrefabPath)
            throw new InvalidOperationException("HUD prefab is not connected to the scene.");

        string[] requiredReferences =
        {
            "healthFill", "experienceFill", "rCooldownOverlay", "healthText", "experienceText",
            "attackText", "attackSpeedText", "moveSpeedText", "rCountdownText"
        };
        SerializedObject serializedHud = new SerializedObject(hud);
        foreach (string field in requiredReferences)
        {
            if (serializedHud.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("HUD reference is missing: " + field);
        }

        Debug.Log("Player HUD validation passed.");
    }
}
