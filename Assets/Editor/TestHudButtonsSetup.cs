using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TestHudButtonsSetup
{
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string WhiteSpritePath = "Assets/03. Asset/Sprites/WhitePixel.png";
    private const string PickupPrefabPath = "Assets/04. Prefabs/Items/ExperiencePickup.prefab";

    public static void Restore()
    {
        Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (hud == null)
            throw new InvalidOperationException("Prototype_Test is missing PlayerHUD.");
        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        ExperiencePickup2D pickup = AssetDatabase.LoadAssetAtPath<ExperiencePickup2D>(PickupPrefabPath);
        Transform hpText = hud.transform.Find("HP Text");
        Font font = hpText != null ? hpText.GetComponent<Text>().font : null;
        if (white == null || pickup == null || font == null)
            throw new InvalidOperationException("Test button sprite, font, or pickup prefab is missing.");

        Button healthPlus = CreateButton(hud.transform, "HP +10", 25f, font, white);
        Button healthMinus = CreateButton(hud.transform, "HP -10", 85f, font, white);
        Button expPlus = CreateButton(hud.transform, "EXP +10%", 145f, font, white);
        Button levelUp = CreateButton(hud.transform, "레벨 +1", 205f, font, white);
        Button spawn = CreateButton(hud.transform, "경험치 더미 생성", 265f, font, white);
        hud.ConfigureDebugButtons(healthPlus, healthMinus, expPlus);
        hud.ConfigureLevelUpButton(levelUp);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        ExperiencePickupSpawner spawner = hud.GetComponent<ExperiencePickupSpawner>();
        if (spawner == null)
            spawner = hud.gameObject.AddComponent<ExperiencePickupSpawner>();
        spawner.Configure(spawn, pickup);
        PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("Prototype_Test buttons restored; Prototype_Play remains clean.");
    }

    private static Button CreateButton(Transform root, string label, float top,
        Font font, Sprite white)
    {
        Transform existing = root.Find(label + " Border/" + label);
        if (existing != null)
            return existing.GetComponent<Button>();
        GameObject border = new GameObject(label + " Border", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(root, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = Vector2.one;
        borderRect.pivot = Vector2.one;
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
        captionRect.offsetMin = captionRect.offsetMax = Vector2.zero;
        Text text = caption.GetComponent<Text>();
        text.font = font;
        text.fontSize = label == "경험치 더미 생성" ? 18 : 22;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
        text.raycastTarget = false;
        return button;
    }

    public static void Validate()
    {
        EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        PlayerHudUI testHud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (testHud == null || testHud.GetComponentsInChildren<Button>(true).Length != 5 ||
            testHud.GetComponent<ExperiencePickupSpawner>() == null)
            throw new InvalidOperationException("Prototype_Test must have five working test buttons.");
        SerializedObject serialized = new SerializedObject(testHud);
        foreach (string field in new[] { "healthPlusButton", "healthMinusButton",
                     "experiencePlusButton", "levelUpButton" })
        {
            if (serialized.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("Test button reference is missing: " + field);
        }
        EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        PlayerHudUI playHud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (playHud == null || playHud.GetComponentsInChildren<Button>(true).Length != 0 ||
            playHud.GetComponent<ExperiencePickupSpawner>() != null)
            throw new InvalidOperationException("Prototype_Play still contains test buttons.");
        Debug.Log("Test-only HUD button separation validated.");
    }
}
