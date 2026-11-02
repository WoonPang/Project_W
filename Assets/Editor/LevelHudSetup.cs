using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelHudSetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";
    private const string PlayerPrefabPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string HudPrefabPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string CircleSpritePath = "Assets/03. Asset/Sprites/HudCircle.png";

    public static void Apply()
    {
        AddPlayerProgress();
        AddLevelBadgeAndOverlap();
        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Level badge and EXP rollover created.");
    }

    private static void AddPlayerProgress()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (player.GetComponent<PlayerProgress>() == null)
                player.AddComponent<PlayerProgress>();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static void AddLevelBadgeAndOverlap()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            if (hud.transform.Find("Level Badge Border") != null)
                throw new InvalidOperationException("Level badge already exists.");

            RectTransform expBorder = Child<RectTransform>(hud.transform, "EXP Border");
            RectTransform expFill = Child<RectTransform>(hud.transform, "EXP Fill");
            expBorder.anchoredPosition = new Vector2(105f, -74f);
            expBorder.sizeDelta = new Vector2(586f, 50f);
            expFill.anchoredPosition = new Vector2(107f, -76f);
            expFill.sizeDelta = new Vector2(582f, 46f);

            Transform portraitBorder = hud.transform.Find("Portrait Border");
            Transform portraitPlaceholder = hud.transform.Find("Portrait Placeholder");
            if (portraitBorder == null || portraitPlaceholder == null)
                throw new InvalidOperationException("Portrait elements are missing.");
            portraitBorder.SetAsLastSibling();
            portraitPlaceholder.SetAsLastSibling();

            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            Font font = Child<Text>(hud.transform, "HP Text").font;
            if (circle == null || font == null)
                throw new InvalidOperationException("Level badge sprite or font is missing.");

            CreateImage(hud.transform, "Level Badge Border", circle, Color.black,
                125f, 125f, 50f, 50f);
            CreateImage(hud.transform, "Level Badge Fill", circle, Color.white,
                127f, 127f, 46f, 46f);
            Text levelText = CreateText(hud.transform, "Level Number", font, "1",
                28, Color.black, 125f, 125f, 50f, 50f);
            hud.GetComponent<PlayerHudUI>().ConfigureLevelText(levelText);

            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static T Child<T>(Transform root, string name) where T : Component
    {
        Transform child = root.Find(name);
        if (child == null || !child.TryGetComponent(out T component))
            throw new InvalidOperationException("HUD component is missing: " + name);
        return component;
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
        int fontSize, Color color, float x, float y, float width, float height)
    {
        GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        element.transform.SetParent(parent, false);
        SetRect(element.GetComponent<RectTransform>(), x, y, width, height);
        Text label = element.GetComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
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

    private static void Validate()
    {
        PlayerProgress progress = UnityEngine.Object.FindFirstObjectByType<PlayerProgress>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (progress == null || hud == null)
            throw new InvalidOperationException("Player progress or HUD is missing from Prototype.");
        if (new SerializedObject(hud).FindProperty("levelText").objectReferenceValue == null)
            throw new InvalidOperationException("Level badge number is not connected to the HUD.");

        RectTransform expBorder = Child<RectTransform>(hud.transform, "EXP Border");
        RectTransform expFill = Child<RectTransform>(hud.transform, "EXP Fill");
        if (!Mathf.Approximately(expBorder.anchoredPosition.x, 105f) ||
            !Mathf.Approximately(expFill.anchoredPosition.x, 107f))
            throw new InvalidOperationException("EXP bar was not moved beneath the portrait.");

        GameObject testObject = new GameObject("Progress Validation");
        try
        {
            PlayerProgress testProgress = testObject.AddComponent<PlayerProgress>();
            testProgress.ResetProgress();
            for (int i = 0; i < 9; i++)
                testProgress.AddExperiencePercent(10f);
            if (testProgress.CurrentLevel != 1 || !Mathf.Approximately(testProgress.ExperiencePercent, 90f))
                throw new InvalidOperationException("EXP did not reach 90% after nine clicks.");
            testProgress.AddExperiencePercent(10f);
            if (testProgress.CurrentLevel != 2 || !Mathf.Approximately(testProgress.ExperiencePercent, 0f))
                throw new InvalidOperationException("EXP did not reset when the player leveled up.");
            testProgress.AddExperiencePercent(10f);
            if (testProgress.CurrentLevel != 2 || !Mathf.Approximately(testProgress.ExperiencePercent, 10f))
                throw new InvalidOperationException("EXP did not continue after leveling up.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }

        Debug.Log("Level badge and EXP rollover validation passed.");
    }
}
