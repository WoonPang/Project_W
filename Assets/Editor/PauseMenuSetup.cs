using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PauseMenuSetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string HudPrefabPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string PausePrefabPath = "Assets/04. Prefabs/UI/PauseMenu.prefab";
    private const string WhiteSpritePath = "Assets/03. Asset/Sprites/WhitePixel.png";

    public static void Apply()
    {
        if (AssetDatabase.LoadMainAssetAtPath(PausePrefabPath) != null)
            throw new InvalidOperationException("Pause menu already exists.");

        MoveExperienceFill();
        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (white == null || font == null)
            throw new InvalidOperationException("UI sprite or font is missing.");
        GameObject pausePrefab = CreatePausePrefab(white, font);

        var scene = EditorSceneManager.OpenScene(ScenePath);
        PrefabUtility.InstantiatePrefab(pausePrefab, scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Validate();
        Debug.Log("Visible 10% EXP fill and ESC pause menu created.");
    }

    private static void MoveExperienceFill()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            Transform fill = hud.transform.Find("EXP Fill");
            if (fill == null)
                throw new InvalidOperationException("EXP Fill is missing.");

            RectTransform rect = fill.GetComponent<RectTransform>();
            // At 10%, the bar ends near x=213; the portrait ends near x=175.
            rect.anchoredPosition = new Vector2(160f, -76f);
            rect.sizeDelta = new Vector2(529f, 46f);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static GameObject CreatePausePrefab(Sprite white, Font font)
    {
        GameObject root = new GameObject("Pause Menu", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PauseMenuUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlay = new GameObject("Pause Overlay", typeof(RectTransform),
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

        GameObject mainPanel = CreatePanel(overlay.transform, "Main Pause Panel", white);
        Button resume = CreateButton(mainPanel.transform, "Resume", 30f, white, font);
        Button option = CreateButton(mainPanel.transform, "Option", 185f, white, font);
        Button quit = CreateButton(mainPanel.transform, "Quit", 340f, white, font);

        GameObject optionPanel = CreatePanel(overlay.transform, "Option Panel", white);
        Text optionTitle = CreateText(optionPanel.transform, "Option Title", "Option", font, 70,
            25f, 35f, 350f, 120f);
        optionTitle.color = Color.white;
        Button back = CreateButton(optionPanel.transform, "Back", 340f, white, font);
        optionPanel.SetActive(false);

        root.GetComponent<PauseMenuUI>().Configure(overlay, mainPanel, optionPanel,
            resume, option, quit, back);
        overlay.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (prefab == null)
            throw new InvalidOperationException("Pause menu prefab could not be saved.");
        return prefab;
    }

    private static GameObject CreatePanel(Transform parent, string name, Sprite white)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(400f, 500f);
        Image image = panel.GetComponent<Image>();
        image.sprite = white;
        image.color = new Color(0.48f, 0.48f, 0.48f);
        return panel;
    }

    private static Button CreateButton(Transform parent, string label, float top,
        Sprite white, Font font)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetTopLeftRect(buttonObject.GetComponent<RectTransform>(), 25f, top, 350f, 120f);
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = white;
        image.color = new Color(0.86f, 0.86f, 0.86f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        CreateText(buttonObject.transform, "Label", label, font, 72,
            0f, 0f, 350f, 120f);
        return button;
    }

    private static Text CreateText(Transform parent, string name, string value,
        Font font, int fontSize, float x, float y, float width, float height)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        SetTopLeftRect(textObject.GetComponent<RectTransform>(), x, y, width, height);
        Text label = textObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.text = value;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;
        return label;
    }

    private static void SetTopLeftRect(RectTransform rect, float x, float y,
        float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Validate()
    {
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        PauseMenuUI pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuUI>();
        if (hud == null || pause == null)
            throw new InvalidOperationException("HUD or pause menu is missing from Prototype.");
        if (pause.GetComponent<Canvas>().sortingOrder <= hud.GetComponent<Canvas>().sortingOrder)
            throw new InvalidOperationException("Pause menu is not above the HUD.");

        RectTransform fill = hud.transform.Find("EXP Fill").GetComponent<RectTransform>();
        if (!Mathf.Approximately(fill.anchoredPosition.x, 160f) ||
            !Mathf.Approximately(fill.sizeDelta.x, 529f) ||
            fill.anchoredPosition.x + 0.1f * fill.sizeDelta.x <= 175f)
            throw new InvalidOperationException("The first 10% of EXP is still hidden by the portrait.");

        SerializedObject serializedPause = new SerializedObject(pause);
        foreach (string field in new[] { "overlayRoot", "mainPanel", "optionPanel",
                     "resumeButton", "optionButton", "quitButton", "backButton" })
        {
            if (serializedPause.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("Pause menu reference is missing: " + field);
        }

        Image overlayImage = pause.transform.Find("Pause Overlay").GetComponent<Image>();
        if (!Mathf.Approximately(overlayImage.color.a, 0.75f))
            throw new InvalidOperationException("Pause overlay opacity is not 75%.");

        float originalTimeScale = Time.timeScale;
        pause.OpenMenu();
        if (!pause.IsPaused || !Mathf.Approximately(Time.timeScale, 0f))
            throw new InvalidOperationException("ESC menu did not pause the game.");
        pause.ResumeGame();
        if (pause.IsPaused || !Mathf.Approximately(Time.timeScale, originalTimeScale))
            throw new InvalidOperationException("Resume did not restore game time.");

        Debug.Log("Pause menu and EXP visibility validation passed.");
    }
}
