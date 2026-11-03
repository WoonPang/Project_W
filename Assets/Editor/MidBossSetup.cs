using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MidBossSetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestPlayScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string BossPrefabPath = "Assets/04. Prefabs/Enemies/MidBoss.prefab";
    private const string MeleePrefabPath = "Assets/04. Prefabs/Enemies/BasicMeleeEnemy.prefab";
    private const string RangedPrefabPath = "Assets/04. Prefabs/Enemies/BasicRangedEnemy.prefab";
    private const string SquareSpritePath = "Assets/03. Asset/Sprites/BasicMeleeEnemy.png";
    private const string CircleSpritePath = "Assets/03. Asset/Sprites/BasicRangedEnemy.png";
    private const string PentagonSpritePath = "Assets/03. Asset/Sprites/TankMeleeEnemy.png";

    public static void Create()
    {
        MidBoss2D prefab = CreatePrefab();
        ConfigureScene(PlayScenePath, prefab);
        ConfigureScene(TestPlayScenePath, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Five-minute mid boss and both play scenes are ready.");
    }

    private static MidBoss2D CreatePrefab()
    {
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        Sprite pentagon = AssetDatabase.LoadAssetAtPath<Sprite>(PentagonSpritePath);
        BasicMeleeEnemy2D melee = AssetDatabase.LoadAssetAtPath<BasicMeleeEnemy2D>(MeleePrefabPath);
        BasicRangedEnemy2D ranged = AssetDatabase.LoadAssetAtPath<BasicRangedEnemy2D>(RangedPrefabPath);
        if (square == null || circle == null || pentagon == null || melee == null || ranged == null)
            throw new InvalidOperationException("Mid boss sprites or summon prefabs are missing.");

        GameObject root = new GameObject("MidBoss");
        try
        {
            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 1.08f;
            collider.isTrigger = true;
            root.AddComponent<EnemyStatus2D>();

            SpriteRenderer circleBoundary = CreatePart(root.transform, "Slam Boundary", circle,
                new Color(1f, 0.1f, 0.12f, 0.18f), Vector3.one * 6f, 4);
            SpriteRenderer circleFill = CreatePart(root.transform, "Slam Fill", circle,
                new Color(1f, 0.08f, 0.1f, 0.38f), Vector3.one * 0.1f, 5);
            SpriteRenderer dashBoundary = CreatePart(root.transform, "Dash Boundary", square,
                new Color(1f, 0.12f, 0.15f, 0.25f), Vector3.one, 6);
            SpriteRenderer dashFill = CreatePart(root.transform, "Dash Fill", square,
                new Color(1f, 0.13f, 0.15f, 0.4f), Vector3.one, 7);
            circleBoundary.enabled = false;
            circleFill.enabled = false;
            dashBoundary.enabled = false;
            dashFill.enabled = false;

            CreatePart(root.transform, "Boss Outline", pentagon,
                new Color(1f, 0.63f, 0.65f), Vector3.one * 2.45f, 8);
            SpriteRenderer body = CreatePart(root.transform, "Boss Body", pentagon,
                new Color(0.9f, 0.38f, 0.42f), Vector3.one * 2.2f, 9);
            CreatePart(root.transform, "Boss Core", circle,
                new Color(1f, 0.94f, 0.86f, 0.9f), Vector3.one * 0.54f, 10);

            SpriteRenderer barBack = CreatePart(root.transform, "Health Bar Background", square,
                new Color(0.08f, 0.12f, 0.15f), new Vector3(2.4f, 0.19f, 1f), 11);
            barBack.transform.localPosition = new Vector3(0f, 1.53f, -0.01f);
            SpriteRenderer barFill = CreatePart(root.transform, "Health Bar Fill", square,
                new Color(0.35f, 0.92f, 0.47f), new Vector3(2.26f, 0.12f, 1f), 12);
            barFill.transform.localPosition = new Vector3(0f, 1.54f, -0.02f);

            root.AddComponent<MidBoss2D>().Configure(body, barFill,
                circleBoundary, circleFill, dashBoundary, dashFill, melee, ranged);
            return PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath).GetComponent<MidBoss2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer CreatePart(Transform parent, string name, Sprite sprite,
        Color color, Vector3 scale, int sortingOrder)
    {
        GameObject part = new GameObject(name, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localScale = scale;
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void ConfigureScene(string path, MidBoss2D prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        if (camera == null || player == null || clock == null)
            throw new InvalidOperationException("Mid boss scene needs a camera, player, and clock: " + path);
        MidBossSpawner2D spawner = UnityEngine.Object.FindFirstObjectByType<MidBossSpawner2D>();
        if (spawner == null)
            spawner = new GameObject("Mid Boss Spawner").AddComponent<MidBossSpawner2D>();
        spawner.Configure(prefab, camera, player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Validate()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        try
        {
            MidBoss2D boss = prefab.GetComponent<MidBoss2D>();
            CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
            if (boss == null || boss.MaxHealth != 150f || boss.AttackDamage != 10f ||
                boss.MoveSpeed != 1.25f || boss.ExperienceOnDeath != 50f ||
                boss.SlamDamage != 17.5f || boss.DashDamage != 12.5f ||
                collider == null || !collider.isTrigger ||
                prefab.GetComponent<EnemyStatus2D>() == null ||
                prefab.transform.Find("Boss Body")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Health Bar Fill")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Slam Boundary")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Slam Fill")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Dash Boundary")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Dash Fill")?.GetComponent<SpriteRenderer>() == null ||
                new SerializedObject(boss).FindProperty("slamCooldownSeconds").floatValue != 5f ||
                new SerializedObject(boss).FindProperty("dashCooldownSeconds").floatValue != 10f ||
                new SerializedObject(boss).FindProperty("summonCooldownSeconds").floatValue != 30f ||
                new SerializedObject(boss).FindProperty("stopDistance").floatValue != 2.1f ||
                new SerializedObject(boss).FindProperty("slamStartRange").floatValue != 2.3f ||
                new SerializedObject(boss).FindProperty("slamRadius").floatValue != 4.5f ||
                new SerializedObject(boss).FindProperty("slamWarningSeconds").floatValue != 0.8f ||
                new SerializedObject(boss).FindProperty("dashDistance").floatValue != 12f ||
                new SerializedObject(boss).FindProperty("dashSpeed").floatValue != 16f ||
                new SerializedObject(boss).FindProperty("patternStartRange").floatValue != 7f ||
                new SerializedObject(boss).FindProperty("dashHoldSeconds").floatValue != 0.5f ||
                new SerializedObject(boss).FindProperty("meleePrefab").objectReferenceValue !=
                    AssetDatabase.LoadAssetAtPath<BasicMeleeEnemy2D>(MeleePrefabPath) ||
                new SerializedObject(boss).FindProperty("rangedPrefab").objectReferenceValue !=
                    AssetDatabase.LoadAssetAtPath<BasicRangedEnemy2D>(RangedPrefabPath))
                throw new InvalidOperationException("Mid boss prefab is incomplete.");
            if (!MidBoss2D.IsPointNearSegment(Vector2.zero, Vector2.right * 2f,
                    new Vector2(1f, 0.5f), 0.85f) ||
                MidBoss2D.IsPointNearSegment(Vector2.zero, Vector2.right * 2f,
                    new Vector2(1f, 1.1f), 0.85f))
                throw new InvalidOperationException("Mid boss dash hit area is wrong.");
            if (EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.MidBoss, 11) != 525f)
                throw new InvalidOperationException("Mid boss level scaling is wrong.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        ValidateScene(PlayScenePath, false);
        ValidateScene(TestPlayScenePath, true);
        ValidateSlamAndSummon();
        ValidatePatternCooldowns();
        Scene test = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (!test.IsValid() ||
            UnityEngine.Object.FindFirstObjectByType<MidBossSpawner2D>() != null)
            throw new InvalidOperationException("Prototype_Test should remain a dummy-only scene.");
        Debug.Log("Mid boss waves, level HP, summon-first and distance-based patterns, close-range dodgeable slam, dash hold, cooldowns, and summons validated.");
    }

    private static void ValidateScene(string path, bool testButtons)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        MidBossSpawner2D spawner = UnityEngine.Object.FindFirstObjectByType<MidBossSpawner2D>();
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (!scene.IsValid() || spawner == null || clock == null || hud == null ||
            spawner.FirstSpawnAfterSeconds != 300f || spawner.SpawnCount != 2 ||
            spawner.SecondSpawnAfterSeconds != 600f || spawner.SecondSpawnCount != 3 ||
            spawner.BossSeparation != 3.5f ||
            new SerializedObject(spawner).FindProperty("bossPrefab").objectReferenceValue !=
                AssetDatabase.LoadAssetAtPath<MidBoss2D>(BossPrefabPath) ||
            (hud.transform.Find("+5min Border") != null) != testButtons)
            throw new InvalidOperationException("Mid boss spawner or test HUD is wrong in " + path);
        if (!testButtons)
            return;

        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            MethodInfo update = typeof(MidBossSpawner2D).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            clock.AdvanceSeconds(299f);
            update.Invoke(spawner, null);
            if (UnityEngine.Object.FindFirstObjectByType<MidBoss2D>() != null)
                throw new InvalidOperationException("Mid boss spawned before five minutes.");
            clock.AdvanceSeconds(1f);
            update.Invoke(spawner, null);
            MidBoss2D[] bosses = UnityEngine.Object.FindObjectsByType<MidBoss2D>(
                FindObjectsSortMode.None);
            if (!spawner.HasSpawned || bosses.Length != 2)
                throw new InvalidOperationException("Two mid bosses did not spawn at five minutes.");
            Vector3 firstViewport = Camera.main.WorldToViewportPoint(bosses[0].transform.position);
            Vector3 secondViewport = Camera.main.WorldToViewportPoint(bosses[1].transform.position);
            foreach (MidBoss2D boss in bosses)
            {
                Vector3 viewport = Camera.main.WorldToViewportPoint(boss.transform.position);
                if (viewport.x >= 0f && viewport.x <= 1f &&
                    viewport.y >= 0f && viewport.y <= 1f ||
                    Vector2.Distance(boss.transform.position, player.transform.position) < 8f)
                    throw new InvalidOperationException("Mid boss spawned on-screen or beside the player.");
            }
            bool sameSide = firstViewport.x < 0f && secondViewport.x < 0f ||
                firstViewport.x > 1f && secondViewport.x > 1f ||
                firstViewport.y < 0f && secondViewport.y < 0f ||
                firstViewport.y > 1f && secondViewport.y > 1f;
            float bossDistance = Vector2.Distance(
                bosses[0].transform.position, bosses[1].transform.position);
            if (!sameSide || bossDistance < 2.5f || bossDistance > 6f)
                throw new InvalidOperationException("Mid bosses did not spawn together on one side.");
            clock.AdvanceSeconds(299f);
            update.Invoke(spawner, null);
            if (UnityEngine.Object.FindObjectsByType<MidBoss2D>(
                    FindObjectsSortMode.None).Length != 2)
                throw new InvalidOperationException("Mid bosses spawned before ten minutes.");
            clock.AdvanceSeconds(1f);
            update.Invoke(spawner, null);
            if (!spawner.HasSecondWaveSpawned || UnityEngine.Object.FindObjectsByType<MidBoss2D>(
                    FindObjectsSortMode.None).Length != 5)
                throw new InvalidOperationException("Three mid bosses did not spawn at ten minutes.");
            clock.AdvanceSeconds(60f);
            update.Invoke(spawner, null);
            MidBoss2D[] allBosses = UnityEngine.Object.FindObjectsByType<MidBoss2D>(
                FindObjectsSortMode.None);
            if (allBosses.Length != 5)
                throw new InvalidOperationException("Mid boss wave spawned more than once.");
            foreach (MidBoss2D boss in allBosses)
                UnityEngine.Object.DestroyImmediate(boss.gameObject);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private static void ValidateSlamAndSummon()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
        Vector3 originalPosition = player.transform.position;
        try
        {
            instance.transform.position = originalPosition + Vector3.right * 2f;
            MidBoss2D boss = instance.GetComponent<MidBoss2D>();
            InvokeAwake(boss);
            Type type = typeof(MidBoss2D);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetMethod("BeginSlam", flags).Invoke(boss, null);
            SpriteRenderer circleFill = instance.transform.Find("Slam Fill")
                .GetComponent<SpriteRenderer>();
            if (!boss.IsChargingSlam || !circleFill.enabled || player.CurrentHealth != 100f)
                throw new InvalidOperationException("Mid boss slam did not warn before damage.");
            type.GetField("actionStartedAt", flags).SetValue(boss, Time.time - 1.3f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (boss.IsChargingSlam || player.CurrentHealth != 82.5f)
                throw new InvalidOperationException("Mid boss slam did not deal 17.5 damage.");

            type.GetMethod("SummonMinions", flags).Invoke(boss, null);
            BasicMeleeEnemy2D[] melee = UnityEngine.Object.FindObjectsByType<BasicMeleeEnemy2D>(
                FindObjectsSortMode.None);
            BasicRangedEnemy2D[] ranged = UnityEngine.Object.FindObjectsByType<BasicRangedEnemy2D>(
                FindObjectsSortMode.None);
            if (melee.Length != 3 || ranged.Length != 3)
                throw new InvalidOperationException("Mid boss did not summon three of each enemy.");
            foreach (BasicMeleeEnemy2D enemy in melee)
            {
                if (Vector2.Distance(enemy.transform.position, player.transform.position) < 3.5f)
                    throw new InvalidOperationException("Mid boss summoned a melee enemy beside the player.");
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
            foreach (BasicRangedEnemy2D enemy in ranged)
            {
                if (Vector2.Distance(enemy.transform.position, player.transform.position) < 3.5f)
                    throw new InvalidOperationException("Mid boss summoned a ranged enemy beside the player.");
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }
        finally
        {
            player.transform.position = originalPosition;
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ValidatePatternCooldowns()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
        float previousTimeScale = Time.timeScale;
        Vector3 previousPlayerPosition = player.transform.position;
        try
        {
            Time.timeScale = 1f;
            instance.transform.position = player.transform.position + Vector3.right * 2.2f;
            MidBoss2D boss = instance.GetComponent<MidBoss2D>();
            InvokeAwake(boss);
            Type type = typeof(MidBoss2D);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo action = type.GetField("action", flags);
            FieldInfo startedAt = type.GetField("actionStartedAt", flags);
            FieldInfo nextAction = type.GetField("nextActionAt", flags);
            FieldInfo nextSlam = type.GetField("nextSlamAt", flags);
            FieldInfo nextDash = type.GetField("nextDashAt", flags);
            FieldInfo nextSummon = type.GetField("nextSummonAt", flags);
            MethodInfo choose = type.GetMethod("TryStartPattern", flags);
            MethodInfo finish = type.GetMethod("EndAction", flags);
            float now = Time.time;

            nextAction.SetValue(boss, now - 1f);
            nextSlam.SetValue(boss, now + 100f);
            nextDash.SetValue(boss, now + 100f);
            nextSummon.SetValue(boss, now + 100f);
            choose.Invoke(boss, null);
            if (action.GetValue(boss).ToString() != "Chasing")
                throw new InvalidOperationException("Mid boss used a pattern on cooldown.");

            nextSlam.SetValue(boss, now - 1f);
            choose.Invoke(boss, null);
            if (!boss.IsChargingSlam)
                throw new InvalidOperationException("Mid boss did not choose the ready slam.");
            startedAt.SetValue(boss, now - 0.5f);
            choose.Invoke(boss, null);
            if (!boss.IsChargingSlam || (float)startedAt.GetValue(boss) != now - 0.5f)
                throw new InvalidOperationException("Mid boss started overlapping patterns.");
            finish.Invoke(boss, new object[] { false });
            if (Mathf.Abs((float)nextSlam.GetValue(boss) - (Time.time + 5f)) > 0.1f)
                throw new InvalidOperationException("Mid boss slam cooldown is wrong.");

            nextAction.SetValue(boss, now - 1f);
            choose.Invoke(boss, null);
            if (action.GetValue(boss).ToString() != "Chasing")
                throw new InvalidOperationException("Mid boss reused slam during cooldown.");
            nextDash.SetValue(boss, now - 1f);
            choose.Invoke(boss, null);
            if (!boss.IsChargingDash)
                throw new InvalidOperationException("Mid boss did not choose the ready dash.");
            startedAt.SetValue(boss, Time.time - 1.1f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (!boss.IsWaitingToDash || boss.IsDashing ||
                !instance.transform.Find("Dash Fill").GetComponent<SpriteRenderer>().enabled)
                throw new InvalidOperationException("Mid boss skipped its half-second dash hold.");
            Vector2 lockedDirection = (Vector2)type.GetField("dashDirection", flags).GetValue(boss);
            Vector2 heldPosition = instance.GetComponent<Rigidbody2D>().position;
            player.transform.position += Vector3.up * 2f;
            type.GetMethod("FixedUpdate", flags).Invoke(boss, null);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (!boss.IsWaitingToDash ||
                Vector2.Distance(instance.GetComponent<Rigidbody2D>().position, heldPosition) > 0.001f ||
                Vector2.Distance((Vector2)type.GetField("dashDirection", flags).GetValue(boss),
                    lockedDirection) > 0.001f)
                throw new InvalidOperationException("Mid boss moved or retargeted during dash hold.");
            startedAt.SetValue(boss, Time.time - 0.51f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (!boss.IsDashing ||
                Vector2.Distance((Vector2)type.GetField("dashDirection", flags).GetValue(boss),
                    lockedDirection) > 0.001f)
                throw new InvalidOperationException("Mid boss did not dash along the locked warning line.");
            player.transform.position = previousPlayerPosition;
            finish.Invoke(boss, new object[] { true });
            if (Mathf.Abs((float)nextDash.GetValue(boss) - (Time.time + 10f)) > 0.1f)
                throw new InvalidOperationException("Mid boss dash cooldown is wrong.");

            nextAction.SetValue(boss, now - 1f);
            nextSlam.SetValue(boss, now - 1f);
            nextDash.SetValue(boss, now - 1f);
            nextSummon.SetValue(boss, now - 1f);
            choose.Invoke(boss, null);
            if (action.GetValue(boss).ToString() != "SummonWarning")
                throw new InvalidOperationException("Mid boss did not prioritize the ready summon.");
            finish.Invoke(boss, new object[] { true });
            if (Mathf.Abs((float)nextSummon.GetValue(boss) - (Time.time + 30f)) > 0.1f)
                throw new InvalidOperationException("Mid boss summon cooldown is wrong.");

            instance.transform.position = player.transform.position + Vector3.right * 5f;
            nextAction.SetValue(boss, now - 1f);
            nextDash.SetValue(boss, now - 1f);
            choose.Invoke(boss, null);
            if (!boss.IsChargingDash)
                throw new InvalidOperationException("Mid boss did not dash when player was far away.");
            finish.Invoke(boss, new object[] { true });
            instance.transform.position = player.transform.position + Vector3.right * 2.2f;
            nextAction.SetValue(boss, now - 1f);
            nextSlam.SetValue(boss, now - 1f);
            choose.Invoke(boss, null);
            if (!boss.IsChargingSlam)
                throw new InvalidOperationException("Mid boss did not slam when player was close.");
            finish.Invoke(boss, new object[] { true });

            GameObject secondInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
            try
            {
                secondInstance.transform.position = player.transform.position + Vector3.left * 2.2f;
                MidBoss2D secondBoss = secondInstance.GetComponent<MidBoss2D>();
                InvokeAwake(secondBoss);
                nextAction.SetValue(boss, now - 1f);
                nextSlam.SetValue(boss, now - 1f);
                choose.Invoke(boss, null);
                if (!boss.IsChargingSlam)
                    throw new InvalidOperationException("First mid boss did not start its pattern.");
                nextAction.SetValue(secondBoss, now - 1f);
                nextDash.SetValue(secondBoss, now + 100f);
                nextSummon.SetValue(secondBoss, now + 100f);
                choose.Invoke(secondBoss, null);
                if (!secondBoss.IsChargingSlam)
                    throw new InvalidOperationException("Second mid boss did not start independently.");
                finish.Invoke(boss, new object[] { true });
                if (!secondBoss.IsChargingSlam)
                    throw new InvalidOperationException("First mid boss changed the second boss pattern.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondInstance);
            }
        }
        finally
        {
            player.transform.position = previousPlayerPosition;
            Time.timeScale = previousTimeScale;
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void InvokeAwake(Component component)
    {
        component.GetType().GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    }
}
