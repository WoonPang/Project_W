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

            FinalBoss2D boss = root.AddComponent<FinalBoss2D>();
            boss.Configure(bossBody, healthFill,
                laserBoundary, laserFill, laserBeam, summonGlow, meteorBoundaries,
                meteorFills, meteorFalling, meteorImpacts, projectile, enemies);
            SerializedObject bossSettings = new SerializedObject(boss);
            bossSettings.FindProperty("meteorChargeSeconds").floatValue = 0.8f;
            bossSettings.FindProperty("meteorPatternSeconds").floatValue = 5f;
            bossSettings.ApplyModifiedPropertiesWithoutUndo();
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
                serialized.FindProperty("meteorChargeSeconds").floatValue != 0.8f ||
                serialized.FindProperty("meteorPatternSeconds").floatValue != 5f ||
                serialized.FindProperty("meteorBoundaries").arraySize != 24 ||
                serialized.FindProperty("scatterProjectileCount").intValue != 24 ||
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
        ValidateDistancePriority();
        ValidatePatterns();
        ValidateIndependentBossPatterns();
        EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindFirstObjectByType<FinalBossSpawner2D>() != null)
            throw new InvalidOperationException("Final boss spawner leaked into Prototype_Test.");
        Debug.Log("Final boss stats, 24-shot radial attack, half-health summon, stationary charges, five-second staggered meteors, and independent patterns validated.");
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
            if (shots.Length != 24)
                throw new InvalidOperationException("Final boss radial attack did not fire 24 shots.");
            Vector2[] directions = new Vector2[shots.Length];
            float[] angles = new float[shots.Length];
            for (int index = 0; index < shots.Length; index++)
            {
                directions[index] = (Vector2)typeof(EnemyProjectile2D)
                    .GetField("direction", flags).GetValue(shots[index]);
                angles[index] = Mathf.Repeat(Mathf.Atan2(directions[index].y,
                    directions[index].x) * Mathf.Rad2Deg, 360f);
                float damage = (float)typeof(EnemyProjectile2D)
                    .GetField("damage", flags).GetValue(shots[index]);
                if (Mathf.Abs(damage - 20f) > 0.001f)
                    throw new InvalidOperationException("Final boss scatter bullets must deal 20 damage each.");
                for (int other = 0; other < index; other++)
                {
                    float angle = Vector2.Angle(directions[index], directions[other]);
                    if (angle < 14f)
                        throw new InvalidOperationException("Final boss scatter lanes overlap.");
                }
            }
            Array.Sort(angles);
            for (int index = 0; index < angles.Length; index++)
            {
                float next = index + 1 < angles.Length ? angles[index + 1] : angles[0] + 360f;
                if (next - angles[index] > 16f)
                    throw new InvalidOperationException("Final boss radial attack left a large gap.");
            }

            typeof(FinalBoss2D).GetField("nextScatterAt", flags).SetValue(boss, 0f);
            typeof(FinalBoss2D).GetMethod("BeginLaserWarning", flags).Invoke(boss, null);
            Vector2 chargePosition = instance.GetComponent<Rigidbody2D>().position;
            typeof(FinalBoss2D).GetMethod("FireScatterAttack", flags).Invoke(boss, null);
            typeof(FinalBoss2D).GetMethod("FixedUpdate", flags).Invoke(boss, null);
            typeof(FinalBoss2D).GetMethod("Update", flags).Invoke(boss, null);
            if (UnityEngine.Object.FindObjectsByType<EnemyProjectile2D>(
                    FindObjectsSortMode.None).Length != 24 ||
                Vector2.Distance(instance.GetComponent<Rigidbody2D>().position, chargePosition) > 0.001f)
                throw new InvalidOperationException("Final boss moved or fired during laser charge.");
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

    private static void ValidateDistancePriority()
    {
        EditorSceneManager.OpenScene(TestPlayScenePath, OpenSceneMode.Single);
        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        InvokeAwake(player);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath));
        try
        {
            FinalBoss2D boss = instance.GetComponent<FinalBoss2D>();
            InvokeAwake(boss);
            Type type = typeof(FinalBoss2D);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("nextActionAt", flags).SetValue(boss, Time.time - 1f);
            instance.transform.position = player.transform.position + Vector3.right * 7f;
            type.GetMethod("TryStartPattern", flags).Invoke(boss, null);
            if (!boss.IsChargingLaser)
                throw new InvalidOperationException("Final boss did not prioritize laser at long range.");
            type.GetMethod("CancelAction", flags).Invoke(boss, null);
            type.GetField("nextActionAt", flags).SetValue(boss, Time.time - 1f);
            instance.transform.position = player.transform.position + Vector3.right * 4f;
            type.GetMethod("TryStartPattern", flags).Invoke(boss, null);
            if (!boss.IsChargingMeteor)
                throw new InvalidOperationException("Final boss did not prioritize meteors at close range.");
            Vector2 chargePosition = instance.GetComponent<Rigidbody2D>().position;
            type.GetMethod("FixedUpdate", flags).Invoke(boss, null);
            if (Vector2.Distance(instance.GetComponent<Rigidbody2D>().position, chargePosition) > 0.001f)
                throw new InvalidOperationException("Final boss moved during meteor charge.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
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
            type.GetMethod("TryStartPattern", flags).Invoke(boss, null);
            if (action.GetValue(boss).ToString() != "LaserFiring" ||
                (float)startedAt.GetValue(boss) != laserFiredAt)
                throw new InvalidOperationException("Final boss started overlapping patterns.");
            MethodInfo finish = type.GetMethod("EndAction", flags);
            finish.Invoke(boss, null);
            if (Mathf.Abs((float)type.GetField("nextLaserAt", flags).GetValue(boss) -
                    (Time.time + 7f)) > 0.1f)
                throw new InvalidOperationException("Final boss laser cooldown is wrong.");

            type.GetMethod("BeginMeteorStorm", flags).Invoke(boss, null);
            if (!boss.IsChargingMeteor ||
                !instance.transform.Find("Meteor 0 Boundary").GetComponent<SpriteRenderer>().enabled ||
                instance.transform.Find("Meteor 1 Boundary").GetComponent<SpriteRenderer>().enabled)
                throw new InvalidOperationException("Final boss meteors did not begin sequentially.");
            Vector2[] meteorPositions = (Vector2[])type.GetField("meteorPositions", flags)
                .GetValue(boss);
            int meteorCount = (int)type.GetField("activeMeteorCount", flags).GetValue(boss);
            if (meteorCount != 24)
                throw new InvalidOperationException("Final boss meteor count is wrong.");
            Camera camera = Camera.main;
            float interval = (5f - 0.8f - 0.3f) / (meteorCount - 1);
            meteorPositions[0] = player.transform.position;
            typeof(PlayerStats).GetField("invulnerableUntil", flags).SetValue(player, -1f);
            type.GetMethod("FireScatterAttack", flags).Invoke(boss, null);
            if (UnityEngine.Object.FindObjectsByType<EnemyProjectile2D>(
                    FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Final boss fired its basic attack during meteors.");
            type.GetField("actionStartedAt", flags).SetValue(boss, Time.time - 0.85f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (!boss.IsChargingMeteor ||
                !instance.transform.Find("Meteor 0 Impact").GetComponent<SpriteRenderer>().enabled ||
                instance.transform.Find("Meteor 23 Impact").GetComponent<SpriteRenderer>().enabled ||
                Mathf.Abs(player.CurrentHealth - 46f) > 0.001f)
                throw new InvalidOperationException("Final boss meteors did not land one by one.");
            for (int index = 1; index < meteorCount; index++)
            {
                startedAt.SetValue(boss, Time.time - (0.85f + index * interval));
                type.GetMethod("Update", flags).Invoke(boss, null);
            }
            for (int first = 0; first < meteorCount; first++)
            {
                Vector3 viewport = camera.WorldToViewportPoint(meteorPositions[first]);
                if (viewport.z <= 0f || viewport.x <= 0f || viewport.x >= 1f ||
                    viewport.y <= 0f || viewport.y >= 1f)
                    throw new InvalidOperationException("A meteor landed outside the view.");
                for (int second = first + 1; second < meteorCount; second++)
                    if ((second - first) * interval < 1.1f &&
                        Vector2.Distance(meteorPositions[first], meteorPositions[second]) < 2.45f)
                        throw new InvalidOperationException("Simultaneous meteor warnings overlap.");
            }
            startedAt.SetValue(boss, Time.time - 5.1f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (boss.IsChargingMeteor)
                throw new InvalidOperationException("Final boss meteor pattern exceeded five seconds.");
            if (Mathf.Abs((float)type.GetField("nextMeteorAt", flags).GetValue(boss) -
                    (Time.time + 15f)) > 0.1f)
                throw new InvalidOperationException("Final boss meteor cooldown is wrong.");

            boss.TakeDamage(boss.MaxHealth * 0.5f);
            if (!boss.IsSummonInvulnerable ||
                boss.CurrentHealth != boss.MaxHealth * 0.5f)
                throw new InvalidOperationException("Final boss did not enter its half-health summon phase.");
            boss.TakeDamage(20f);
            if (boss.CurrentHealth != boss.MaxHealth * 0.5f)
                throw new InvalidOperationException("Final boss took damage while summoning.");
            type.GetMethod("TryStartPattern", flags).Invoke(boss, null);
            if (!boss.IsChargingSummon)
                throw new InvalidOperationException("Final boss did not prioritize its half-health summon.");
            MonsterSpawnGate2D.SetPaused(true);
            startedAt.SetValue(boss, Time.time - 1.1f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (boss.IsSummonInvulnerable || boss.HasSummonedAtHalfHealth)
                throw new InvalidOperationException("Paused spawning consumed the final boss summon.");
            MonsterSpawnGate2D.SetPaused(false);
            type.GetMethod("TryStartPattern", flags).Invoke(boss, null);
            if (!boss.IsChargingSummon)
                throw new InvalidOperationException("Final boss did not retry its queued summon.");
            startedAt.SetValue(boss, Time.time - 1.1f);
            type.GetMethod("Update", flags).Invoke(boss, null);
            if (!boss.HasSummonedAtHalfHealth || boss.IsSummonInvulnerable)
                throw new InvalidOperationException("Final boss summon did not finish once.");
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
            type.GetField("nextActionAt", flags).SetValue(boss, Time.time - 1f);
            type.GetField("nextLaserAt", flags).SetValue(boss, Time.time + 100f);
            type.GetField("nextMeteorAt", flags).SetValue(boss, Time.time + 100f);
            type.GetMethod("TryStartPattern", flags).Invoke(boss, null);
            if (boss.IsChargingSummon)
                throw new InvalidOperationException("Final boss repeated its half-health summon.");
        }
        finally
        {
            MonsterSpawnGate2D.SetPaused(false);
            foreach (CombatTarget2D target in UnityEngine.Object.FindObjectsByType<CombatTarget2D>(
                         FindObjectsSortMode.None))
                if (target.gameObject != instance)
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
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
            midType.GetField("nextActionAt", flags).SetValue(midBoss, now - 1f);
            midType.GetField("nextSlamAt", flags).SetValue(midBoss, now - 1f);
            midType.GetField("nextDashAt", flags).SetValue(midBoss, now + 100f);

            finalType.GetMethod("TryStartPattern", flags).Invoke(finalBoss, null);
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
