using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HudRevisionSetup
{
    private const string HudPrefabPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string WhiteSpritePath = "Assets/03. Asset/Sprites/WhitePixel.png";
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";

    public static void Apply()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            if (hud.transform.Find("HP +10 Border") != null)
                throw new InvalidOperationException("HUD test buttons already exist.");

            Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            Font font = Child<Text>(hud.transform, "HP Text").font;
            if (white == null || font == null)
                throw new InvalidOperationException("HUD sprite or font is missing.");

            Transform topStrip = hud.transform.Find("Q Slot Top");
            if (topStrip != null)
                UnityEngine.Object.DestroyImmediate(topStrip.gameObject);

            Rename(hud.transform, "Q Slot Border", "R Slot Border");
            Rename(hud.transform, "Q Slot", "R Slot");
            Rename(hud.transform, "Q Cooldown Overlay", "R Cooldown Overlay");
            Rename(hud.transform, "Q Countdown", "R Countdown");
            Rename(hud.transform, "Q Key Label", "R Key Label");

            Child<Image>(hud.transform, "R Slot").color = new Color(0.85f, 0.85f, 0.85f);
            Child<Text>(hud.transform, "R Key Label").text = "R";

            Button healthPlus = CreateButton(hud.transform, "HP +10", font, white, 25f);
            Button healthMinus = CreateButton(hud.transform, "HP -10", font, white, 85f);
            Button expPlus = CreateButton(hud.transform, "EXP +10%", font, white, 145f);

            PlayerHudUI controller = hud.GetComponent<PlayerHudUI>();
            controller.Configure(
                Child<Image>(hud.transform, "HP Fill"),
                Child<Image>(hud.transform, "EXP Fill"),
                Child<Image>(hud.transform, "R Cooldown Overlay"),
                Child<Text>(hud.transform, "HP Text"),
                Child<Text>(hud.transform, "EXP Text"),
                Child<Text>(hud.transform, "Attack Stat"),
                Child<Text>(hud.transform, "Attack Speed Stat"),
                Child<Text>(hud.transform, "Move Speed Stat"),
                Child<Text>(hud.transform, "R Countdown"));
            controller.ConfigureDebugButtons(healthPlus, healthMinus, expPlus);
            Child<Image>(hud.transform, "EXP Fill").fillAmount = 0f;
            Child<Text>(hud.transform, "EXP Text").text = "EXP 0%";

            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("R HUD and health/experience test buttons created.");
    }

    private static void Rename(Transform root, string oldName, string newName)
    {
        Transform child = root.Find(oldName);
        if (child == null)
            throw new InvalidOperationException("HUD element is missing: " + oldName);
        child.name = newName;
    }

    private static T Child<T>(Transform root, string name) where T : Component
    {
        Transform child = root.Find(name);
        if (child == null || !child.TryGetComponent(out T component))
            throw new InvalidOperationException("HUD component is missing: " + name);
        return component;
    }

    private static Button CreateButton(Transform root, string label, Font font, Sprite white, float top)
    {
        GameObject border = new GameObject(label + " Border", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(root, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(1f, 1f);
        borderRect.anchorMax = new Vector2(1f, 1f);
        borderRect.pivot = new Vector2(1f, 1f);
        borderRect.anchoredPosition = new Vector2(-25f, -top);
        borderRect.sizeDelta = new Vector2(170f, 50f);
        Image borderImage = border.GetComponent<Image>();
        borderImage.sprite = white;
        borderImage.color = Color.black;
        borderImage.raycastTarget = false;

        GameObject face = new GameObject(label, typeof(RectTransform),
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
        captionText.text = label;
        captionText.raycastTarget = false;
        return button;
    }

    private static void Validate()
    {
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        RSkillCooldown rSkill = UnityEngine.Object.FindFirstObjectByType<RSkillCooldown>();
        if (hud == null || rSkill == null || rSkill.CooldownSeconds != 30f)
            throw new InvalidOperationException("R cooldown or HUD is missing from Prototype.");
        EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null || eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            throw new InvalidOperationException("Input System UI event module is missing.");

        SerializedObject serializedHud = new SerializedObject(hud);
        foreach (string field in new[] { "healthPlusButton", "healthMinusButton", "experiencePlusButton",
                     "rCooldownOverlay", "rCountdownText" })
        {
            if (serializedHud.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("HUD field is missing: " + field);
        }
        if (Child<Image>(hud.transform, "EXP Fill").fillAmount != 0f)
            throw new InvalidOperationException("EXP should start at zero for the button test.");
        if (hud.transform.Find("R Slot Top") != null || hud.transform.Find("Q Slot Top") != null)
            throw new InvalidOperationException("The old slot top strip remains.");

        Debug.Log("R HUD revision validation passed.");
    }
}
