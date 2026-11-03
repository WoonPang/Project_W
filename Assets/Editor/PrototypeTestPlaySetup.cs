using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrototypeTestPlaySetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string TestPlayScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string PickupPrefabPath = "Assets/04. Prefabs/Items/ExperiencePickup.prefab";
    private const string EnemyPrefabFolder = "Assets/04. Prefabs/Enemies/";
    private static readonly string[] MobLabels =
        { "기본 근거리", "근접 탱커", "근접 암살자", "기본 원거리", "차징 원거리" };
    private static readonly string[] MobPrefabNames =
        { "BasicMeleeEnemy", "TankMeleeEnemy", "AssassinMeleeEnemy",
          "BasicRangedEnemy", "ChargingRangedEnemy" };

    public static void Create()
    {
        Scene playScene = EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        EnsureGameTime(playScene);
        EditorSceneManager.SaveScene(playScene);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestPlayScenePath) == null &&
            !AssetDatabase.CopyAsset(PlayScenePath, TestPlayScenePath))
            throw new InvalidOperationException("Could not copy Prototype_Play to Prototype_Test_Play.");

        Scene testPlayScene = EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        GameSessionTime2D gameTime = EnsureGameTime(testPlayScene);
        PlayerHudUI hud = FindHud(testPlayScene);
        if (hud == null)
            throw new InvalidOperationException("Prototype_Test_Play has no HUD.");

        if (hud.GetComponent<TestPlayTimeControls>() == null)
        {
            Scene testScene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            PlayerHudUI testHud = FindHud(testScene);
            if (testHud == null)
                throw new InvalidOperationException("Prototype_Test has no test HUD.");

            GameObject hudCopy = UnityEngine.Object.Instantiate(testHud.gameObject);
            if (hudCopy.scene != testPlayScene)
                SceneManager.MoveGameObjectToScene(hudCopy, testPlayScene);
            hudCopy.name = "PlayerHUD";
            UnityEngine.Object.DestroyImmediate(hud.gameObject);
            EditorSceneManager.CloseScene(testScene, true);
            SceneManager.SetActiveScene(testPlayScene);

            hud = hudCopy.GetComponent<PlayerHudUI>();
            ConfigureTestHud(hud);
            Button oneMinute = CreateTimeButton(hud.transform, "+1min", -325f);
            Button fiveMinutes = CreateTimeButton(hud.transform, "+5min", -385f);
            hudCopy.AddComponent<TestPlayTimeControls>().Configure(
                oneMinute, fiveMinutes, gameTime, hudCopy.GetComponent<PlayTimeUI>());
        }

        EnsureDebugControls(hud);

        EditorSceneManager.MarkSceneDirty(testPlayScene);
        EditorSceneManager.SaveScene(testPlayScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Prototype_Test_Play scene, test HUD, and shared time skip buttons are ready.");
    }

    private static GameSessionTime2D EnsureGameTime(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameSessionTime2D existing = root.GetComponent<GameSessionTime2D>();
            if (existing != null)
                return existing;
        }

        GameObject clockObject = new GameObject("Game Session Time", typeof(GameSessionTime2D));
        if (clockObject.scene != scene)
            SceneManager.MoveGameObjectToScene(clockObject, scene);
        EditorSceneManager.MarkSceneDirty(scene);
        return clockObject.GetComponent<GameSessionTime2D>();
    }

    private static PlayerHudUI FindHud(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlayerHudUI hud = root.GetComponentInChildren<PlayerHudUI>(true);
            if (hud != null)
                return hud;
        }
        return null;
    }

    private static Button FindButton(Transform root, string label)
    {
        return root.Find(label + " Border/" + label)?.GetComponent<Button>();
    }

    private static void ConfigureTestHud(PlayerHudUI hud)
    {
        Transform root = hud.transform;
        Button healthPlus = FindButton(root, "HP +10");
        Button healthMinus = FindButton(root, "HP -10");
        Button experiencePlus = FindButton(root, "EXP +10%");
        Button levelUp = FindButton(root, "레벨 +1");
        Button pickupButton = FindButton(root, "경험치 더미 생성");
        ExperiencePickup2D pickup = AssetDatabase.LoadAssetAtPath<ExperiencePickup2D>(PickupPrefabPath);
        if (healthPlus == null || healthMinus == null || experiencePlus == null ||
            levelUp == null || pickupButton == null || pickup == null)
            throw new InvalidOperationException("Prototype_Test debug buttons or pickup prefab are missing.");

        hud.ConfigureDebugButtons(healthPlus, healthMinus, experiencePlus);
        hud.ConfigureLevelUpButton(levelUp);
        ExperiencePickupSpawner pickupSpawner = hud.GetComponent<ExperiencePickupSpawner>();
        if (pickupSpawner == null)
            pickupSpawner = hud.gameObject.AddComponent<ExperiencePickupSpawner>();
        pickupSpawner.Configure(pickupButton, pickup);
        PlayTimeUI display = hud.GetComponent<PlayTimeUI>();
        Text timer = root.Find("Play Time")?.GetComponent<Text>();
        if (display == null || timer == null)
            throw new InvalidOperationException("Prototype_Test play-time HUD is missing.");
        display.Configure(timer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        PrefabUtility.RecordPrefabInstancePropertyModifications(pickupSpawner);
        PrefabUtility.RecordPrefabInstancePropertyModifications(display);
    }

    private static Button CreateTimeButton(Transform root, string label, float anchoredY)
    {
        Transform template = root.Find("레벨 +1 Border");
        if (template == null)
            throw new InvalidOperationException("Level test button is missing from the test HUD.");
        GameObject border = UnityEngine.Object.Instantiate(template.gameObject, root, false);
        border.name = label + " Border";
        border.GetComponent<RectTransform>().anchoredPosition = new Vector2(-25f, anchoredY);
        Transform face = border.transform.GetChild(0);
        face.name = label;
        Text caption = face.Find("Label")?.GetComponent<Text>();
        Button button = face.GetComponent<Button>();
        if (caption == null || button == null)
            throw new InvalidOperationException("Copied time button has no label or Button component.");
        caption.text = label;
        return button;
    }

    private static void EnsureDebugControls(PlayerHudUI hud)
    {
        Transform root = hud.transform;
        Button invincible = CreateDebugButton(root, root, "무적", -215f, -25f);
        Button mobMenu = CreateDebugButton(root, root, "잡몹 소환", -215f, -85f);
        Button midBoss = CreateDebugButton(root, root, "중간보스 소환", -215f, -145f);
        Button finalBoss = CreateDebugButton(root, root, "보스 소환", -215f, -205f);
        Button spawnToggle = CreateDebugButton(root, root, "몬스터 스폰 정지", -215f, -265f);
        Button clear = CreateDebugButton(root, root, "모든 몬스터 제거", -215f, -325f);

        Transform menu = root.Find("잡몹 종류 선택");
        if (menu == null)
        {
            GameObject menuObject = new GameObject("잡몹 종류 선택", typeof(RectTransform));
            menu = menuObject.transform;
            menu.SetParent(root, false);
            RectTransform rect = (RectTransform)menu;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        Button[] mobChoices = new Button[MobLabels.Length];
        CombatTarget2D[] mobPrefabs = new CombatTarget2D[MobLabels.Length];
        for (int index = 0; index < MobLabels.Length; index++)
        {
            mobChoices[index] = CreateDebugButton(root, menu, MobLabels[index],
                -405f, -85f - 60f * index);
            mobPrefabs[index] = LoadEnemy<CombatTarget2D>(MobPrefabNames[index]);
        }
        menu.gameObject.SetActive(false);

        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (player == null || camera == null)
            throw new InvalidOperationException("Prototype_Test_Play player or camera is missing.");
        TestPlayDebugControls controls = hud.GetComponent<TestPlayDebugControls>();
        if (controls == null)
            controls = hud.gameObject.AddComponent<TestPlayDebugControls>();
        controls.Configure(invincible, mobMenu, midBoss, finalBoss, spawnToggle, clear,
            menu.gameObject, mobChoices, mobPrefabs,
            LoadEnemy<MidBoss2D>("MidBoss"), LoadEnemy<FinalBoss2D>("FinalBoss"),
            player, camera);
        PrefabUtility.RecordPrefabInstancePropertyModifications(controls);
    }

    private static T LoadEnemy<T>(string name) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            EnemyPrefabFolder + name + ".prefab");
        T component = prefab != null ? prefab.GetComponent<T>() : null;
        if (component == null)
            throw new InvalidOperationException("Missing enemy prefab: " + name);
        return component;
    }

    private static Button CreateDebugButton(Transform hudRoot, Transform parent,
        string label, float anchoredX, float anchoredY)
    {
        Transform border = parent.Find(label + " Border");
        if (border == null)
        {
            Transform template = hudRoot.Find("레벨 +1 Border");
            if (template == null)
                throw new InvalidOperationException("Level test button is missing.");
            border = UnityEngine.Object.Instantiate(template.gameObject, parent, false).transform;
            border.name = label + " Border";
        }
        RectTransform rect = border.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(anchoredX, anchoredY);
        Transform face = border.GetChild(0);
        face.name = label;
        Text caption = face.Find("Label")?.GetComponent<Text>();
        Button button = face.GetComponent<Button>();
        if (caption == null || button == null)
            throw new InvalidOperationException("Copied debug button is incomplete: " + label);
        caption.text = label;
        caption.fontSize = 18;
        return button;
    }

    public static void Validate()
    {
        Scene playScene = EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        PlayerHudUI playHud = FindHud(playScene);
        if (playHud == null || playHud.GetComponent<TestPlayTimeControls>() != null ||
            playHud.GetComponent<TestPlayDebugControls>() != null ||
            playHud.transform.Find("+1min Border") != null ||
            FindButton(playHud.transform, "HP +10") != null ||
            playScene.GetRootGameObjects().Length == 0)
            throw new InvalidOperationException("Prototype_Play must keep its normal HUD.");

        Scene scene = EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerHudUI hud = FindHud(scene);
        GameSessionTime2D gameTime = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<GameSessionTime2D>() != null)
                gameTime = root.GetComponent<GameSessionTime2D>();
        }
        if (hud == null || gameTime == null ||
            UnityEngine.Object.FindFirstObjectByType<BasicMeleeEnemySpawner2D>() == null ||
            UnityEngine.Object.FindFirstObjectByType<BasicRangedEnemySpawner2D>() == null ||
            UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Prototype_Test_Play must contain the basic monster spawners and no dummies.");

        foreach (string label in new[] { "HP +10", "HP -10", "EXP +10%", "레벨 +1",
                     "경험치 더미 생성", "+1min", "+5min", "무적", "잡몹 소환",
                     "중간보스 소환", "보스 소환", "몬스터 스폰 정지", "모든 몬스터 제거" })
        {
            if (FindButton(hud.transform, label) == null)
                throw new InvalidOperationException("Missing Prototype_Test_Play button: " + label);
        }
        SerializedObject hudSettings = new SerializedObject(hud);
        foreach (var binding in new[] {
                     (Field: "healthPlusButton", Label: "HP +10"),
                     (Field: "healthMinusButton", Label: "HP -10"),
                     (Field: "experiencePlusButton", Label: "EXP +10%"),
                     (Field: "levelUpButton", Label: "레벨 +1") })
        {
            if (hudSettings.FindProperty(binding.Field).objectReferenceValue !=
                FindButton(hud.transform, binding.Label))
                throw new InvalidOperationException("HUD test button is not connected: " + binding.Label);
        }
        TestPlayTimeControls controls = hud.GetComponent<TestPlayTimeControls>();
        TestPlayDebugControls debugControls = hud.GetComponent<TestPlayDebugControls>();
        PlayTimeUI display = hud.GetComponent<PlayTimeUI>();
        Text timer = hud.transform.Find("Play Time")?.GetComponent<Text>();
        if (controls == null || debugControls == null || display == null || timer == null ||
            hud.GetComponent<ExperiencePickupSpawner>() == null)
            throw new InvalidOperationException("Time controls or test HUD components are missing.");
        SerializedObject debugSettings = new SerializedObject(debugControls);
        foreach (var binding in new[] {
                     (Field: "invincibleButton", Label: "무적"),
                     (Field: "mobMenuButton", Label: "잡몹 소환"),
                     (Field: "midBossButton", Label: "중간보스 소환"),
                     (Field: "finalBossButton", Label: "보스 소환"),
                     (Field: "spawnToggleButton", Label: "몬스터 스폰 정지"),
                     (Field: "clearButton", Label: "모든 몬스터 제거") })
        {
            if (debugSettings.FindProperty(binding.Field).objectReferenceValue !=
                FindButton(hud.transform, binding.Label))
                throw new InvalidOperationException("Debug button is not connected: " + binding.Label);
        }
        Transform mobMenu = hud.transform.Find("잡몹 종류 선택");
        SerializedProperty mobButtons = debugSettings.FindProperty("mobButtons");
        SerializedProperty mobPrefabs = debugSettings.FindProperty("mobPrefabs");
        if (mobMenu == null || mobMenu.gameObject.activeSelf ||
            mobButtons.arraySize != MobLabels.Length || mobPrefabs.arraySize != MobLabels.Length)
            throw new InvalidOperationException("The five-choice mob menu is missing or visible by default.");
        for (int index = 0; index < MobLabels.Length; index++)
        {
            Button choice = FindButton(mobMenu, MobLabels[index]);
            if (choice == null || mobButtons.GetArrayElementAtIndex(index).objectReferenceValue != choice ||
                mobPrefabs.GetArrayElementAtIndex(index).objectReferenceValue == null)
                throw new InvalidOperationException("Mob choice is not connected: " + MobLabels[index]);
        }
        if (debugSettings.FindProperty("midBossPrefab").objectReferenceValue == null ||
            debugSettings.FindProperty("finalBossPrefab").objectReferenceValue == null ||
            debugSettings.FindProperty("player").objectReferenceValue == null ||
            debugSettings.FindProperty("worldCamera").objectReferenceValue == null)
            throw new InvalidOperationException("Boss prefab, player, or camera is not connected.");
        if (new SerializedObject(hud.GetComponent<ExperiencePickupSpawner>())
                .FindProperty("spawnButton").objectReferenceValue !=
            FindButton(hud.transform, "경험치 더미 생성"))
            throw new InvalidOperationException("Experience dummy test button is not connected.");

        SerializedObject controlSettings = new SerializedObject(controls);
        if (controlSettings.FindProperty("addOneMinuteButton").objectReferenceValue !=
                FindButton(hud.transform, "+1min") ||
            controlSettings.FindProperty("addFiveMinutesButton").objectReferenceValue !=
                FindButton(hud.transform, "+5min") ||
            controlSettings.FindProperty("gameTime").objectReferenceValue != gameTime ||
            controlSettings.FindProperty("timeDisplay").objectReferenceValue != display)
            throw new InvalidOperationException("Time buttons are not connected to the shared clock.");

        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            controls.AddOneMinute();
            if (gameTime.ElapsedSeconds != 60f || display.ElapsedSeconds != 60f ||
                timer.text != "01:00")
                throw new InvalidOperationException("+1min did not update the play clock.");
            controls.AddFiveMinutes();
            BasicMeleeEnemySpawner2D melee = UnityEngine.Object.FindFirstObjectByType<BasicMeleeEnemySpawner2D>();
            BasicRangedEnemySpawner2D ranged = UnityEngine.Object.FindFirstObjectByType<BasicRangedEnemySpawner2D>();
            if (gameTime.ElapsedSeconds != 360f || timer.text != "06:00" ||
                melee.ElapsedSeconds != 360f || ranged.ElapsedSeconds != 360f)
                throw new InvalidOperationException("+5min did not advance the monster spawn clocks.");
            PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            player.AdjustHealth(player.MaxHealth);
            float fullHealth = player.CurrentHealth;
            debugControls.ToggleInvincibility();
            if (fullHealth <= 0f || !player.IsDebugInvincible ||
                player.ReceiveDamage(10f) || player.CurrentHealth != fullHealth ||
                FindButton(hud.transform, "무적").GetComponentInChildren<Text>().text != "무적: 켜짐")
                throw new InvalidOperationException("Debug invincibility did not block damage or update its label.");
            debugControls.ToggleInvincibility();
            debugControls.ToggleSpawnPause();
            if (!MonsterSpawnGate2D.IsPaused ||
                FindButton(hud.transform, "몬스터 스폰 정지").GetComponentInChildren<Text>().text != "몬스터 스폰 재개")
                throw new InvalidOperationException("Monster spawn pause did not update its state.");
            debugControls.ToggleSpawnPause();
            if (MonsterSpawnGate2D.IsPaused)
                throw new InvalidOperationException("Monster spawn pause did not resume.");
            Time.timeScale = 0f;
            controls.AddOneMinute();
            if (gameTime.ElapsedSeconds != 360f || timer.text != "06:00")
                throw new InvalidOperationException("Time skip worked during pause.");
        }
        finally
        {
            PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            if (player != null)
                player.SetDebugInvincible(false);
            MonsterSpawnGate2D.SetPaused(false);
            Time.timeScale = previousTimeScale;
        }

        Debug.Log("Prototype_Test_Play time and debug buttons, mob menu, invincibility, and spawn pause validated.");
    }
}
