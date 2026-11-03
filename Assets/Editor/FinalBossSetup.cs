using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalBossSetup
{
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";
    private const string TestPlayScenePath = "Assets/01. Scenes/Prototype_Test_Play.unity";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string BossPrefabPath = "Assets/04. Prefabs/Enemies/FinalBoss.prefab";
    private const string ProjectilePrefabPath = "Assets/04. Prefabs/Projectiles/EnemyProjectile.prefab";
    private const string SquareSpritePath = "Assets/03. Asset/Sprites/BasicMeleeEnemy.png";
    private const string CircleSpritePath = "Assets/03. Asset/Sprites/BasicRangedEnemy.png";
    private const string PentagonSpritePath = "Assets/03. Asset/Sprites/TankMeleeEnemy.png";
    private static readonly string[] EnemyPrefabPaths =
    {
        "Assets/04. Prefabs/Enemies/BasicMeleeEnemy.prefab",
        "Assets/04. Prefabs/Enemies/TankMeleeEnemy.prefab",
        "Assets/04. Prefabs/Enemies/AssassinMeleeEnemy.prefab",
        "Assets/04. Prefabs/Enemies/BasicRangedEnemy.prefab",
        "Assets/04. Prefabs/Enemies/ChargingRangedEnemy.prefab",
        "Assets/04. Prefabs/Enemies/MidBoss.prefab"
    };

    public static void Create()
    {
        FinalBoss2D prefab = CreatePrefab();
        ConfigureScene(PlayScenePath, prefab);
        ConfigureScene(TestPlayScenePath, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("Fifteen-minute final boss and both play scenes are ready.");
    }

    private static FinalBoss2D CreatePrefab()
    {
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        Sprite pentagon = AssetDatabase.LoadAssetAtPath<Sprite>(PentagonSpritePath);
        EnemyProjectile2D projectile = AssetDatabase.LoadAssetAtPath<EnemyProjectile2D>(
            ProjectilePrefabPath);
        CombatTarget2D[] enemies = new CombatTarget2D[EnemyPrefabPaths.Length];
        for (int index = 0; index < enemies.Length; index++)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[index]);
            enemies[index] = enemyPrefab != null ? enemyPrefab.GetComponent<CombatTarget2D>() : null;
        }
        if (square == null || circle == null || pentagon == null || projectile == null ||
            Array.Exists(enemies, enemy => enemy == null))
            throw new InvalidOperationException("Final boss sprites or enemy prefabs are missing.");

        GameObject root = new GameObject("FinalBoss");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 1.48f;
            collider.isTrigger = true;
            root.AddComponent<EnemyStatus2D>();

            SpriteRenderer summonGlow = CreatePart(root.transform, "Summon Glow", circle,
                new Color(0.8f, 0.35f, 1f, 0.25f), Vector3.one * 2f, 2);
            SpriteRenderer laserBoundary = CreatePart(root.transform, "Laser Boundary", square,
                new Color(1f, 0.1f, 0.15f, 0.28f), Vector3.one, 3);
            SpriteRenderer laserFill = CreatePart(root.transform, "Laser Fill", square,
                new Color(1f, 0.14f, 0.2f, 0.5f), Vector3.one, 4);
            SpriteRenderer laserBeam = CreatePart(root.transform, "Laser Beam", square,
                new Color(1f, 0.56f, 0.32f, 0.85f), Vector3.one, 5);

            SpriteRenderer[] meteorBoundaries = new SpriteRenderer[24];
            SpriteRenderer[] meteorFills = new SpriteRenderer[24];
            SpriteRenderer[] meteorFalling = new SpriteRenderer[24];
            SpriteRenderer[] meteorImpacts = new SpriteRenderer[24];
            for (int index = 0; index < 24; index++)
            {
                string prefix = "Meteor " + index + " ";
                meteorBoundaries[index] = CreatePart(root.transform, prefix + "Boundary", circle,
                    new Color(1f, 0.12f, 0.1f, 0.26f), Vector3.one * 2.2f, 3);
                meteorFills[index] = CreatePart(root.transform, prefix + "Fill", circle,
                    new Color(1f, 0.2f, 0.1f, 0.4f), Vector3.one * 0.05f, 4);
                meteorFalling[index] = CreatePart(root.transform, prefix + "Falling", circle,
                    new Color(1f, 0.65f, 0.16f), Vector3.one * 0.65f, 5);
                meteorImpacts[index] = CreatePart(root.transform, prefix + "Impact", circle,
                    new Color(1f, 0.7f, 0.3f, 0.85f), Vector3.one * 2.2f, 6);
            }

            SpriteRenderer outline = CreatePart(root.transform, "Boss Outline", square,
                new Color(0.96f, 0.55f, 0.94f), Vector3.one * 3.42f, 8);
            outline.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            SpriteRenderer bossBody = CreatePart(root.transform, "Boss Body", square,
                new Color(0.34f, 0.19f, 0.53f), Vector3.one * 3.12f, 9);
            bossBody.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreatePart(root.transform, "Boss Crest", pentagon,
                new Color(0.8f, 0.46f, 0.88f), Vector3.one * 1.3f, 10);
            CreatePart(root.transform, "Boss Core", circle,
                new Color(1f, 0.82f, 0.4f), Vector3.one * 0.5f, 11);

            SpriteRenderer healthBack = CreatePart(root.transform, "Health Bar Background", square,
                new Color(0.08f, 0.12f, 0.15f), new Vector3(3.4f, 0.23f, 1f), 12);
            healthBack.transform.localPosition = new Vector3(0f, 2.01f, -0.01f);
            SpriteRenderer healthFill = CreatePart(root.transform, "Health Bar Fill", square,
                new Color(0.48f, 0.92f, 0.65f), new Vector3(3.2f, 0.16f, 1f), 13);
            healthFill.transform.localPosition = new Vector3(0f, 2.02f, -0.02f);

            root.AddComponent<FinalBoss2D>().Configure(bossBody, healthFill,
                laserBoundary, laserFill, laserBeam, summonGlow, meteorBoundaries,
                meteorFills, meteorFalling, meteorImpacts, projectile, enemies);
            return PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath).GetComponent<FinalBoss2D>();
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
        renderer.enabled = false;
        if (sortingOrder >= 8)
            renderer.enabled = true;
        return renderer;
    }

    private static void ConfigureScene(string path, FinalBoss2D prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        if (camera == null || player == null ||
            UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>() == null)
            throw new InvalidOperationException("Final boss scene is missing player, camera, or clock.");
        FinalBossSpawner2D spawner = UnityEngine.Object.FindFirstObjectByType<FinalBossSpawner2D>();
        if (spawner == null)
            spawner = new GameObject("Final Boss Spawner").AddComponent<FinalBossSpawner2D>();
        spawner.Configure(prefab, camera, player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Validate()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        try
        {
            FinalBoss2D boss = prefab.GetComponent<FinalBoss2D>();
            SerializedObject serialized = new SerializedObject(boss);
            if (boss == null || boss.MaxHealth != 500f || boss.AttackDamage != 20f ||
                boss.MoveSpeed != 1.5f || boss.ExperienceOnDeath != 100f ||
                prefab.GetComponent<EnemyStatus2D>() == null ||
                prefab.GetComponent<CircleCollider2D>() == null ||
                prefab.transform.Find("Boss Body")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Laser Boundary")?.GetComponent<SpriteRenderer>() == null ||
                prefab.transform.Find("Meteor 0 Falling")?.GetComponent<SpriteRenderer>() == null ||
                serialized.FindProperty("laserCooldownSeconds").floatValue != 7f ||
                serialized.FindProperty("meteorCooldownSeconds").floatValue != 15f ||
                serialized.FindProperty("summonCooldownSeconds").floatValue != 30f ||
                serialized.FindProperty("meteorBoundaries").arraySize != 24 ||
                serialized.FindProperty("scatterProjectileCount").intValue != 10 ||
                Mathf.Abs(serialized.FindProperty("scatterArcDegrees").floatValue - 144f) > 0.001f ||
                serialized.FindProperty("scatterProjectilePrefab").objectReferenceValue !=
                    AssetDatabase.LoadAssetAtPath<EnemyProjectile2D>(ProjectilePrefabPath) ||
                serialized.FindProperty("summonPrefabs").arraySize != 6 ||
                EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.FinalBoss, 11) != 1750f)
                throw new InvalidOperationException("Final boss prefab, PDF stats, or level health is wrong.");
            for (int index = 0; index < EnemyPrefabPaths.Length; index++)
            {
                GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[index]);
                if (serialized.FindProperty("summonPrefabs").GetArrayElementAtIndex(index)
                    .objectReferenceValue != enemy.GetComponent<CombatTarget2D>())
                    throw new InvalidOperationException("Final boss summon roster is incomplete.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        ValidateScene(PlayScenePath, false);
        ValidateScene(TestPlayScenePath, true);
        ValidateScatterAttack();
        ValidatePatterns();
        ValidateIndependentBossPatterns();
        EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindFirstObjectByType<FinalBossSpawner2D>() != null)
            throw new InvalidOperationException("Final boss spawner leaked into Prototype_Test.");
        Debug.Log("Final boss stats, ten-shot scatter attack, fifteen-minute spawn, independent patterns, dense meteors, and mid-boss summons validated.");
    }

    private static void ValidateScene(string path, bool hasTimeButtons)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        FinalBossSpawner2D spawner = UnityEngine.Object.FindFirstObjectByType<FinalBossSpawner2D>();
        if (!scene.IsValid() || spawner == null || spawner.SpawnAfterSeconds != 900f ||
            (UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>().transform.Find("+5min Border") != null)
                != hasTimeButtons)
            throw new InvalidOperationException("Final boss spawner or HUD is wrong in " + path);
        if (!hasTimeButtons)
            return;

        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        GameSessionTime2D clock = UnityEngine.Object.FindFirstObjectByType<GameSessionTime2D>();
        MethodInfo update = typeof(FinalBossSpawner2D).GetMethod("Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            clock.AdvanceSeconds(899f);
            update.Invoke(spawner, null);
            if (UnityEngine.Object.FindFirstObjectByType<FinalBoss2D>() != null)
                throw new InvalidOperationException("Final boss spawned before fifteen minutes.");
            clock.AdvanceSeconds(1f);
            update.Invoke(spawner, null);
            FinalBoss2D boss = UnityEngine.Object.FindFirstObjectByType<FinalBoss2D>();
            if (!spawner.HasSpawned || boss == null ||
                Vector2.Distance(boss.transform.position, player.transform.position) < 9f)
                throw new InvalidOperationException("Final boss did not spawn safely at fifteen minutes.");
            Vector3 viewport = Camera.main.WorldToViewportPoint(boss.transform.position);
            if (viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f)
                throw new InvalidOperationException("Final boss spawned on screen.");
            clock.AdvanceSeconds(60f);
            update.Invoke(spawner, null);
            if (UnityEngine.Object.FindObjectsByType<FinalBoss2D>(
                    FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Final boss spawned more than once.");
            UnityEngine.Object.DestroyImmediate(boss.gameObject);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private static void ValidateScatterAttack()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            instance.transform.position = player.transform.position + Vector3.right * 5f;
            FinalBoss2D boss = instance.GetComponent<FinalBoss2D>();
            InvokeAwake(boss);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FinalBoss2D).GetMethod("Update", flags).Invoke(boss, null);
            EnemyProjectile2D[] shots = UnityEngine.Object.FindObjectsByType<EnemyProjectile2D>(
                FindObjectsSortMode.None);
            if (shots.Length != 10)
                throw new InvalidOperationException("Final boss scatter attack did not fire ten shots.");
            Vector2[] directions = new Vector2[shots.Length];
            float widestAngle = 0f;
            for (int index = 0; index < shots.Length; index++)
            {
                directions[index] = (Vector2)typeof(EnemyProjectile2D)
                    .GetField("direction", flags).GetValue(shots[index]);
                float damage = (float)typeof(EnemyProjectile2D)
                    .GetField("damage", flags).GetValue(shots[index]);
                if (Mathf.Abs(damage - 20f) > 0.001f)
                    throw new InvalidOperationException("Final boss scatter bullets must deal 20 damage each.");
                for (int other = 0; other < index; other++)
                {
                    float angle = Vector2.Angle(directions[index], directions[other]);
                    if (angle < 14f)
                        throw new InvalidOperationException("Final boss scatter lanes overlap.");
                    widestAngle = Mathf.Max(widestAngle, angle);
                }
            }
            if (widestAngle < 120f)
                throw new InvalidOperationException("Final boss scatter fan is too narrow.");

            typeof(FinalBoss2D).GetField("nextScatterAt", flags).SetValue(boss, 0f);
            typeof(FinalBoss2D).GetMethod("BeginLaserWarning", flags).Invoke(boss, null);
            typeof(FinalBoss2D).GetMethod("Update", flags).Invoke(boss, null);
            if (UnityEngine.Object.FindObjectsByType<EnemyProjectile2D>(
                    FindObjectsSortMode.None).Length != 10)
                throw new InvalidOperationException("Final boss fired scatter during another pattern.");
        }
        finally
        {
            foreach (EnemyProjectile2D shot in UnityEngine.Object.FindObjectsByType<EnemyProjectile2D>(
                         FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(shot.gameObject);
            UnityEngine.Object.DestroyImmediate(instance);
            Time.timeScale = previousTimeScale;
        }
    }

    private static void ValidatePatterns()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
        try
        {
            instance.transform.position = player.transform.position + Vector3.right * 5f;
            FinalBoss2D boss = instance.GetComponent<FinalBoss2D>();
            InvokeAwake(boss);
            Type type = typeof(FinalBoss2D);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetMethod("BeginLaserWarning", flags).Invoke(boss, null);
            if (!boss.IsChargingLaser || player.CurrentHealth != 100f)
                throw new InvalidOperationException("Final boss laser did not warn before firing.");
            type.GetField("actionStartedAt", flags).SetValue(boss, Time.time - 1.6f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (boss.IsChargingLaser || player.CurrentHealth != 70f)
                throw new InvalidOperationException("Final boss laser damage is wrong.");
            FieldInfo action = type.GetField("action", flags);
            FieldInfo startedAt = type.GetField("actionStartedAt", flags);
            float laserFiredAt = (float)startedAt.GetValue(boss);
            type.GetMethod("TryStartRandomPattern", flags).Invoke(boss, null);
            if (action.GetValue(boss).ToString() != "LaserFiring" ||
                (float)startedAt.GetValue(boss) != laserFiredAt)
                throw new InvalidOperationException("Final boss started overlapping patterns.");
            MethodInfo finish = type.GetMethod("EndAction", flags);
            finish.Invoke(boss, null);
            if (Mathf.Abs((float)type.GetField("nextLaserAt", flags).GetValue(boss) -
                    (Time.time + 7f)) > 0.1f)
                throw new InvalidOperationException("Final boss laser cooldown is wrong.");

            type.GetMethod("BeginMeteorWarning", flags).Invoke(boss, null);
            if (!boss.IsChargingMeteor ||
                !instance.transform.Find("Meteor 0 Boundary").GetComponent<SpriteRenderer>().enabled)
                throw new InvalidOperationException("Final boss meteors did not warn.");
            Vector2[] meteorPositions = (Vector2[])type.GetField("meteorPositions", flags)
                .GetValue(boss);
            int meteorCount = (int)type.GetField("activeMeteorCount", flags).GetValue(boss);
            Vector2 safePosition = (Vector2)type.GetField("reservedMeteorSafePosition", flags)
                .GetValue(boss);
            if (meteorCount < 7 || meteorCount > 24 ||
                Vector2.Distance(safePosition, player.transform.position) > 5f)
                throw new InvalidOperationException("Final boss meteor coverage or dodge cell is wrong.");
            for (int first = 0; first < meteorCount; first++)
            {
                if (Vector2.Distance(meteorPositions[first], safePosition) < 2.5f)
                    throw new InvalidOperationException("A meteor covered the reserved dodge cell.");
                for (int second = first + 1; second < meteorCount; second++)
                    if (Vector2.Distance(meteorPositions[first], meteorPositions[second]) < 2.5f)
                        throw new InvalidOperationException("Final boss meteors overlap.");
            }
            meteorPositions[0] = player.transform.position;
            typeof(PlayerStats).GetField("invulnerableUntil", flags).SetValue(player, -1f);
            type.GetField("actionStartedAt", flags).SetValue(boss, Time.time - 1.7f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (boss.IsChargingMeteor ||
                !instance.transform.Find("Meteor 0 Impact").GetComponent<SpriteRenderer>().enabled ||
                Mathf.Abs(player.CurrentHealth - 46f) > 0.001f)
                throw new InvalidOperationException("Final boss meteors did not land.");
            finish.Invoke(boss, null);
            if (Mathf.Abs((float)type.GetField("nextMeteorAt", flags).GetValue(boss) -
                    (Time.time + 15f)) > 0.1f)
                throw new InvalidOperationException("Final boss meteor cooldown is wrong.");

            type.GetMethod("BeginSummonWarning", flags).Invoke(boss, null);
            if (!boss.IsChargingSummon)
                throw new InvalidOperationException("Final boss summon warning is missing.");
            finish.Invoke(boss, null);
            if (Mathf.Abs((float)type.GetField("nextSummonAt", flags).GetValue(boss) -
                    (Time.time + 30f)) > 0.1f)
                throw new InvalidOperationException("Final boss summon cooldown is wrong.");

            type.GetMethod("SummonAllMonsters", flags).Invoke(boss, null);
            foreach (string path in EnemyPrefabPaths)
            {
                Type enemyType = AssetDatabase.LoadAssetAtPath<GameObject>(path)
                    .GetComponent<CombatTarget2D>().GetType();
                int count = 0;
                foreach (CombatTarget2D enemy in UnityEngine.Object.FindObjectsByType<CombatTarget2D>(
                             FindObjectsSortMode.None))
                {
                    if (enemy.GetType() == enemyType)
                    {
                        count++;
                        if (Vector2.Distance(enemy.transform.position, player.transform.position) < 3.5f)
                            throw new InvalidOperationException("Final boss summoned a monster beside player.");
                        UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                    }
                }
                if (count != 2)
                    throw new InvalidOperationException("Final boss did not summon two of each monster, including mid-bosses.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ValidateIndependentBossPatterns()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        GameObject finalInstance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
        GameObject midInstance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/04. Prefabs/Enemies/MidBoss.prefab"));
        try
        {
            finalInstance.transform.position = player.transform.position + Vector3.right * 5f;
            midInstance.transform.position = player.transform.position + Vector3.left * 2.2f;
            FinalBoss2D finalBoss = finalInstance.GetComponent<FinalBoss2D>();
            MidBoss2D midBoss = midInstance.GetComponent<MidBoss2D>();
            InvokeAwake(finalBoss);
            InvokeAwake(midBoss);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type finalType = typeof(FinalBoss2D);
            Type midType = typeof(MidBoss2D);
            float now = Time.time;
            finalType.GetField("nextActionAt", flags).SetValue(finalBoss, now - 1f);
            finalType.GetField("nextLaserAt", flags).SetValue(finalBoss, now - 1f);
            finalType.GetField("nextMeteorAt", flags).SetValue(finalBoss, now + 100f);
            finalType.GetField("nextSummonAt", flags).SetValue(finalBoss, now + 100f);
            midType.GetField("nextActionAt", flags).SetValue(midBoss, now - 1f);
            midType.GetField("nextSlamAt", flags).SetValue(midBoss, now - 1f);
            midType.GetField("nextDashAt", flags).SetValue(midBoss, now + 100f);
            midType.GetField("nextSummonAt", flags).SetValue(midBoss, now + 100f);

            finalType.GetMethod("TryStartRandomPattern", flags).Invoke(finalBoss, null);
            midType.GetMethod("TryStartPattern", flags).Invoke(midBoss, null);
            if (!finalBoss.IsChargingLaser || !midBoss.IsChargingSlam)
                throw new InvalidOperationException("Bosses did not use patterns independently.");
            finalType.GetMethod("CancelAction", flags).Invoke(finalBoss, null);
            if (!midBoss.IsChargingSlam)
                throw new InvalidOperationException("Final boss cancellation interrupted the mid boss.");
            midType.GetMethod("CancelAction", flags).Invoke(midBoss, null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(midInstance);
            UnityEngine.Object.DestroyImmediate(finalInstance);
        }
    }

    private static void InvokeAwake(Component component)
    {
        component.GetType().GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    }
}
