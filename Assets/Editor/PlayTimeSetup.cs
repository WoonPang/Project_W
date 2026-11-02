using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayTimeSetup
{
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";
    private const string WhitePath = "Assets/03. Asset/Sprites/WhitePixel.png";

    public static void Apply()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Font font = hud.transform.Find("HP Text").GetComponent<Text>().font;
            Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
            if (font == null || white == null)
                throw new InvalidOperationException("HUD font or sprite is missing.");

            Text timer = AddTimer(hud.transform, font);
            PlayTimeUI clock = hud.GetComponent<PlayTimeUI>();
            if (clock == null)
                clock = hud.AddComponent<PlayTimeUI>();
            clock.Configure(timer);

            Button levelButton = AddLevelUpButton(hud.transform, font, white);
            hud.GetComponent<PlayerHudUI>().ConfigureLevelUpButton(levelButton);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }

        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Play timer and direct level-up test button are ready.");
    }

    private static Text AddTimer(Transform root, Font font)
    {
        Transform existing = root.Find("Play Time");
        GameObject element;
        if (existing != null)
            element = existing.gameObject;
        else
        {
            element = new GameObject("Play Time", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            element.transform.SetParent(root, false);
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(300f, 70f);
        }

        Text label = element.GetComponent<Text>();
        label.font = font;
        label.fontSize = 50;
        label.text = "00:00";
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        Outline outline = element.GetComponent<Outline>();
        if (outline == null)
            outline = element.AddComponent<Outline>();
        outline.effectColor = new Color(0.38f, 0.38f, 0.38f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        return label;
    }

    private static Button AddLevelUpButton(Transform root, Font font, Sprite white)
    {
        Transform existing = root.Find("레벨 +1 Border/레벨 +1");
        if (existing == null)
            existing = root.Find("레벨업 +1 Border/레벨업 +1");
        if (existing != null)
            return existing.GetComponent<Button>();

        GameObject border = new GameObject("레벨 +1 Border", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(root, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(1f, 1f);
        borderRect.pivot = new Vector2(1f, 1f);
        borderRect.anchoredPosition = new Vector2(-25f, -205f);
        borderRect.sizeDelta = new Vector2(170f, 50f);
        Image borderImage = border.GetComponent<Image>();
        borderImage.sprite = white;
        borderImage.color = Color.black;
        borderImage.raycastTarget = false;

        GameObject face = new GameObject("레벨 +1", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        face.transform.SetParent(border.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(2f, 2f);
        faceRect.offsetMax = new Vector2(-2f, -2f);
        Image faceImage = face.GetComponent<Image>();
        faceImage.sprite = white;
        faceImage.color = new Color(0.85f, 0.85f, 0.85f);
        Button button = face.GetComponent<Button>();
        button.targetGraphic = faceImage;

        GameObject caption = new GameObject("Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        caption.transform.SetParent(face.transform, false);
        RectTransform captionRect = caption.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;
        Text captionText = caption.GetComponent<Text>();
        captionText.font = font;
        captionText.fontSize = 22;
        captionText.color = Color.black;
        captionText.alignment = TextAnchor.MiddleCenter;
        captionText.text = "레벨 +1";
        captionText.raycastTarget = false;
        return button;
    }

    private static void Validate()
    {
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        PlayTimeUI clock = UnityEngine.Object.FindFirstObjectByType<PlayTimeUI>();
        PauseMenuUI pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuUI>();
        LevelUpUI levelMenu = UnityEngine.Object.FindFirstObjectByType<LevelUpUI>();
        if (hud == null || clock == null || pause == null || levelMenu == null)
            throw new InvalidOperationException("Prototype is missing a required UI component.");

        Text timer = hud.transform.Find("Play Time")?.GetComponent<Text>();
        Button button = hud.transform.Find("레벨 +1 Border/레벨 +1")?.GetComponent<Button>();
        if (timer == null || timer.text != "00:00" || button == null)
            throw new InvalidOperationException("Timer or direct level-up button is missing.");
        Outline timerOutline = timer.GetComponent<Outline>();
        if (timer.color != Color.white || timerOutline == null ||
            timerOutline.effectColor != new Color(0.38f, 0.38f, 0.38f, 1f) ||
            timerOutline.effectDistance != new Vector2(2f, -2f))
            throw new InvalidOperationException("Timer contrast or outline is incorrect.");
        RectTransform timerRect = timer.rectTransform;
        if (timerRect.anchorMin != new Vector2(0.5f, 1f) || timerRect.sizeDelta != new Vector2(300f, 70f))
            throw new InvalidOperationException("Timer is not centered at the top.");
        if (new SerializedObject(clock).FindProperty("timeText").objectReferenceValue == null ||
            new SerializedObject(hud).FindProperty("levelUpButton").objectReferenceValue == null)
            throw new InvalidOperationException("Timer or level-up button reference is missing.");
        GameObject test = new GameObject("Level-up Validation");
        try
        {
            PlayerProgress progress = test.AddComponent<PlayerProgress>();
            progress.ResetProgress();
            progress.AddExperiencePercent(40f);
            int eventCount = 0;
            progress.LeveledUp += _ => eventCount++;
            progress.LevelUpImmediately();
            if (progress.CurrentLevel != 2 || progress.ExperiencePercent != 0f || eventCount != 1)
                throw new InvalidOperationException("Direct level-up did not reset EXP or fire the level-up event.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(test);
        }

        Debug.Log("Play timer and direct level-up references validated.");
    }
}
