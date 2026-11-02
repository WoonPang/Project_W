using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelUpCombatUISetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string PopupPath = "Assets/04. Prefabs/UI/DamagePopup.prefab";
    private const string MenuPath = "Assets/04. Prefabs/UI/LevelUpMenu.prefab";
    private const string WhitePath = "Assets/03. Asset/Sprites/WhitePixel.png";

    public static void Apply()
    {
        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (white == null || font == null)
            throw new InvalidOperationException("UI sprite or font is missing.");

        GameObject popup = CreatePopup(font);
        UpdateHud(popup.GetComponent<DamagePopupUI>());
        GameObject menu = CreateMenu(white, font);

        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (UnityEngine.Object.FindFirstObjectByType<LevelUpUI>() == null)
            PrefabUtility.InstantiatePrefab(menu, scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Level-up choices, readable stats, and damage popups are ready.");
    }

    private static GameObject CreatePopup(Font font)
    {
        GameObject root = new GameObject("Damage Popup", typeof(RectTransform),
            typeof(CanvasGroup), typeof(DamagePopupUI));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160f, 80f);
        Text label = CreateText(root.transform, "Damage Number", "10", font, 44,
            Color.yellow, 0f, 0f, 160f, 80f);
        label.fontStyle = FontStyle.Bold;
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        root.GetComponent<DamagePopupUI>().Configure(label, root.GetComponent<CanvasGroup>());
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab ?? throw new InvalidOperationException("Damage popup prefab could not be saved.");
    }

    private static void UpdateHud(DamagePopupUI popup)
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            SetRect(Child<RectTransform>(hud.transform, "Stats Background"), 175f, 125f, 350f, 50f);
            SetStat(hud.transform, "Attack Stat", 194f, 132f, 103f);
            SetStat(hud.transform, "Attack Speed Stat", 296f, 132f, 117f);
            SetStat(hud.transform, "Move Speed Stat", 410f, 132f, 112f);
            DamagePopupSpawner spawner = hud.GetComponent<DamagePopupSpawner>();
            if (spawner == null)
                spawner = hud.AddComponent<DamagePopupSpawner>();
            spawner.Configure(popup);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static void SetStat(Transform root, string name, float x, float y, float width)
    {
        Text label = Child<Text>(root, name);
        SetRect(label.rectTransform, x, y, width, 36f);
        label.fontSize = 18;
        label.fontStyle = FontStyle.Bold;
        label.resizeTextForBestFit = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private static GameObject CreateMenu(Sprite white, Font font)
    {
        GameObject root = new GameObject("Level Up Menu", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LevelUpUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlay = new GameObject("Level Up Overlay", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(root.transform, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.sprite = white;
        overlayImage.color = new Color(0.83f, 0.83f, 0.83f, 0.75f);
        overlayImage.raycastTarget = true;

        Button first = CreateCard(overlay.transform, "Augment Card 1", "증강 1",
            "레벨업 시 무작위로 표시됩니다.", -510f, white, font);
        Button second = CreateCard(overlay.transform, "Augment Card 2", "증강 2",
            "레벨업 시 무작위로 표시됩니다.", 0f, white, font);
        Button third = CreateCard(overlay.transform, "Augment Card 3", "증강 3",
            "레벨업 시 무작위로 표시됩니다.", 510f, white, font);
        root.GetComponent<LevelUpUI>().Configure(overlay, first, second, third);
        overlay.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MenuPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab ?? throw new InvalidOperationException("Level-up menu prefab could not be saved.");
    }

    private static Button CreateCard(Transform parent, string name, string title,
        string description, float centerX, Sprite white, Font font)
    {
        GameObject element = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        element.transform.SetParent(parent, false);
        RectTransform cardRect = element.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(centerX, -5f);
        cardRect.sizeDelta = new Vector2(400f, 650f);
        Image image = element.GetComponent<Image>();
        image.sprite = white;
        image.color = new Color(0.48f, 0.48f, 0.48f);
        Button button = element.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject art = new GameObject("Image Placeholder", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        art.transform.SetParent(element.transform, false);
        SetRect(art.GetComponent<RectTransform>(), 25f, 25f, 350f, 300f);
        Image artImage = art.GetComponent<Image>();
        artImage.sprite = white;
        artImage.color = new Color(0.86f, 0.86f, 0.86f);
        artImage.raycastTarget = false;

        Text heading = CreateText(element.transform, "Title", title, font, 50,
            Color.white, 25f, 350f, 350f, 85f);
        heading.fontStyle = FontStyle.Normal;
        Text detail = CreateText(element.transform, "Description", description, font, 30,
            Color.white, 53f, 465f, 300f, 165f);
        detail.fontStyle = FontStyle.Normal;
        detail.alignment = TextAnchor.UpperLeft;
        return button;
    }

    private static Text CreateText(Transform parent, string name, string value, Font font,
        int size, Color color, float x, float y, float width, float height)
    {
        GameObject element = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        element.transform.SetParent(parent, false);
        SetRect(element.GetComponent<RectTransform>(), x, y, width, height);
        Text label = element.GetComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static T Child<T>(Transform root, string name) where T : Component
    {
        Transform child = root.Find(name);
        if (child == null || !child.TryGetComponent(out T component))
            throw new InvalidOperationException("Missing UI component: " + name);
        return component;
    }

    private static void Validate()
    {
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        LevelUpUI menu = UnityEngine.Object.FindFirstObjectByType<LevelUpUI>();
        PauseMenuUI pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuUI>();
        if (hud == null || menu == null || pause == null)
            throw new InvalidOperationException("Prototype scene is missing a UI prefab.");
        if (menu.GetComponent<Canvas>().sortingOrder <= pause.GetComponent<Canvas>().sortingOrder)
            throw new InvalidOperationException("Level-up menu must draw above pause menu.");
        if (hud.GetComponent<DamagePopupSpawner>() == null)
            throw new InvalidOperationException("Damage popup spawner is missing.");
        RectTransform statsBackground = Child<RectTransform>(hud.transform, "Stats Background");
        if (statsBackground.sizeDelta != new Vector2(350f, 50f) ||
            Child<Text>(hud.transform, "Attack Stat").fontSize != 18 ||
            Child<Text>(hud.transform, "Move Speed Stat").fontSize != 18)
            throw new InvalidOperationException("Stat panel size or typography is incorrect.");
        SerializedObject serializedMenu = new SerializedObject(menu);
        foreach (string field in new[] { "overlayRoot", "attackButton", "healthButton", "speedButton" })
        {
            if (serializedMenu.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("Level-up reference is missing: " + field);
        }
        foreach (string field in new[] { "attackButton", "healthButton", "speedButton" })
        {
            Button card = (Button)serializedMenu.FindProperty(field).objectReferenceValue;
            if (card.GetComponent<RectTransform>().sizeDelta != new Vector2(400f, 650f) ||
                card.transform.Find("Image Placeholder") == null ||
                card.transform.Find("Title") == null ||
                card.transform.Find("Description") == null)
                throw new InvalidOperationException("Level-up choice card layout is incomplete: " + field);
        }
        Debug.Log("Level-up UI, HUD stats, and popup references validated.");
    }
}
