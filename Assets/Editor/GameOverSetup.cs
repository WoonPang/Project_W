using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameOverSetup
{
    private const string LobbyPath = "Assets/01. Scenes/Prototype_Lobby.unity";
    private const string PlayPath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestPlayPath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string TestPath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";

    public static void Apply()
    {
        GameObject hudPrefab = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            if (hudPrefab.GetComponent<GameOverUI>() == null)
                hudPrefab.AddComponent<GameOverUI>();
            PrefabUtility.SaveAsPrefabAsset(hudPrefab, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hudPrefab);
        }

        foreach (string path in new[] { PlayPath, TestPlayPath, TestPath })
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
            if (hud == null)
                throw new InvalidOperationException("HUD missing in " + path);
            if (hud.GetComponent<GameOverUI>() == null)
            {
                hud.gameObject.AddComponent<GameOverUI>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            if (UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>() == null)
            {
                new GameObject("Game Session Time", typeof(GameSessionTime2D));
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene);
        }

        CreateLobby();
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
        {
            Entry(LobbyPath), Entry(PlayPath), Entry(TestPlayPath)
        };
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing.path == LobbyPath || existing.path == PlayPath ||
                existing.path == TestPlayPath)
                continue;
            buildScenes.Add(existing);
        }
        if (!buildScenes.Exists(scene => scene.path == TestPath))
            buildScenes.Add(Entry(TestPath, false));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Game Over result flow, lobby, and test damage setup validated.");
    }

    private static EditorBuildSettingsScene Entry(string path, bool enabled = true)
    {
        return new EditorBuildSettingsScene(path, enabled);
    }

    private static void CreateLobby()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyPath) != null)
        {
            Scene existingScene = EditorSceneManager.OpenScene(LobbyPath,
                OpenSceneMode.Single);
            LobbyMenuUI existingMenu = UnityEngine.Object.FindFirstObjectByType<LobbyMenuUI>();
            if (existingMenu == null)
                throw new InvalidOperationException("Lobby menu is missing.");
            Transform existingRoot = existingMenu.transform;
            Button existingPlay = existingRoot.Find("게임 시작")?.GetComponent<Button>();
            Button existingTest = existingRoot.Find("테스트 시작")?.GetComponent<Button>();
            if (existingPlay == null || existingTest == null)
                throw new InvalidOperationException("Lobby start buttons are missing.");
            Button existingExit = existingRoot.Find("Exit")?.GetComponent<Button>();
            if (existingExit == null)
            {
                Font existingFont = existingPlay.GetComponentInChildren<Text>()?.font;
                existingExit = MakeButton("Exit", existingRoot,
                    new Vector2(0f, -380f), existingFont);
            }
            existingMenu.Configure(existingPlay, existingTest, existingExit);
            EditorSceneManager.MarkSceneDirty(existingScene);
            EditorSceneManager.SaveScene(existingScene);
            return;
        }
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        GameObject camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = new Color(0.09f, 0.1f, 0.12f);

        GameObject root = new GameObject("Lobby Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(LobbyMenuUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Image background = Element("Background", root.transform,
            Vector2.zero, new Vector2(1920f, 1080f), true).GetComponent<Image>();
        background.color = new Color(0.10f, 0.11f, 0.14f);
        background.raycastTarget = false;
        MakeText("PROJECT W", root.transform, new Vector2(0f, 180f),
            new Vector2(1000f, 170f), 120, Color.white, font);
        MakeText("Prototype Lobby", root.transform, new Vector2(0f, 65f),
            new Vector2(600f, 70f), 45, new Color(0.75f, 0.78f, 0.83f), font);

        Button play = MakeButton("게임 시작", root.transform,
            new Vector2(0f, -100f), font);
        Button test = MakeButton("테스트 시작", root.transform,
            new Vector2(0f, -240f), font);
        Button exit = MakeButton("Exit", root.transform,
            new Vector2(0f, -380f), font);
        root.GetComponent<LobbyMenuUI>().Configure(play, test, exit);

        GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        EventSystem eventSystem = eventObject.GetComponent<EventSystem>();
        eventSystem.firstSelectedGameObject = play.gameObject;
        EditorSceneManager.SaveScene(scene, LobbyPath);
    }

    private static Button MakeButton(string caption, Transform parent,
        Vector2 position, Font font)
    {
        RectTransform rect = Element(caption, parent, position,
            new Vector2(510f, 105f), true);
        Image image = rect.GetComponent<Image>();
        image.color = new Color(0.83f, 0.84f, 0.87f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        MakeText(caption, rect, Vector2.zero,
            new Vector2(500f, 100f), 55, Color.black, font);
        return button;
    }

    private static void MakeText(string caption, Transform parent, Vector2 position,
        Vector2 size, int fontSize, Color color, Font font)
    {
        RectTransform rect = Element(caption + " Text", parent, position, size, false);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = caption;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
    }

    private static RectTransform Element(string name, Transform parent,
        Vector2 position, Vector2 size, bool image)
    {
        GameObject element = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer));
        element.transform.SetParent(parent, false);
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        if (image)
            element.AddComponent<Image>();
        return rect;
    }

    private static void Validate()
    {
        GameOverResult2D result = new GameOverResult2D(5, 90.9f, 7);
        if (result.DisplayTime != "01:30" || result.LevelScore != 75 ||
            result.TimeScore != 270 || result.KillScore != 14 ||
            result.TotalScore != 359 || result.Coins != 36 ||
            GameOverResult2D.CoinsFromScore(1) != 1 ||
            GameOverResult2D.CoinsFromScore(10) != 1 ||
            GameOverResult2D.CoinsFromScore(11) != 2)
            throw new InvalidOperationException("Game Over score calculation failed.");
        foreach (string path in new[] { PlayPath, TestPlayPath, TestPath })
        {
            EditorSceneManager.OpenScene(path);
            if (UnityEngine.Object.FindFirstObjectByType<GameOverUI>() == null ||
                UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>() == null)
                throw new InvalidOperationException("Game Over flow missing in " + path);
        }
        EditorSceneManager.OpenScene(LobbyPath);
        LobbyMenuUI lobby = UnityEngine.Object.FindFirstObjectByType<LobbyMenuUI>();
        if (lobby == null || lobby.transform.Find("Exit") == null)
            throw new InvalidOperationException("Lobby scene is missing.");
    }
}
