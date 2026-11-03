using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatFeedbackSetup
{
    private const string PlayerPrefabPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string HudPrefabPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string TestScenePath = "Assets/01. Scenes/Prototype_Test.unity";
    private const string PlayScenePath = "Assets/01. Scenes/Prototype_Play.unity";

    public static void Apply()
    {
        AddPlayerFeedback();
        AddHudFeedback();
        ConfigureTestDummies();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Overkill damage, test dummy variants, player flash, and invulnerability are ready.");
    }

    private static void AddPlayerFeedback()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (player.GetComponent<PlayerHitFeedback2D>() == null)
                player.AddComponent<PlayerHitFeedback2D>();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static void AddHudFeedback()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            if (hud.GetComponent<PlayerDamageFeedbackUI>() == null)
                hud.AddComponent<PlayerDamageFeedbackUI>();
            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static void ConfigureTestDummies()
    {
        Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        foreach (TrainingDummy2D dummy in UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
                     FindObjectsSortMode.None))
        {
            bool infinite = dummy.name.Contains("South 5.1") || dummy.name.Contains("West 7.2");
            bool moving = dummy.name.Contains("East 4.1") || dummy.name.Contains("West 7.2");
            dummy.SetInfiniteHealth(infinite);
            PrefabUtility.RecordPrefabInstancePropertyModifications(dummy);
            if (infinite)
                AddInfiniteMarker(dummy);
            if (moving)
                EnsurePatrol(dummy);
            else if (dummy.GetComponent<TrainingDummyPatrol2D>() != null)
                throw new InvalidOperationException("A static dummy has a patrol component: " + dummy.name);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddInfiniteMarker(TrainingDummy2D dummy)
    {
        SpriteRenderer body = dummy.GetComponent<SpriteRenderer>();
        body.color = new Color(0.58f, 0.9f, 1f, 1f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        Transform old = dummy.transform.Find("Infinite Health Label");
        if (old != null)
            return;
        GameObject marker = new GameObject("Infinite Health Label", typeof(TextMesh));
        marker.transform.SetParent(dummy.transform, false);
        marker.transform.localPosition = new Vector3(0f, 1.12f, -0.02f);
        TextMesh text = marker.GetComponent<TextMesh>();
        text.text = "∞";
        text.fontSize = 64;
        text.characterSize = 0.12f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = new Color(0.58f, 0.9f, 1f);
        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 20;
    }

    private static void EnsurePatrol(TrainingDummy2D dummy)
    {
        Rigidbody2D body = dummy.GetComponent<Rigidbody2D>();
        if (body == null)
            body = dummy.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (dummy.GetComponent<TrainingDummyPatrol2D>() == null)
            dummy.gameObject.AddComponent<TrainingDummyPatrol2D>();
    }

    public static void Validate()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (player.GetComponent<PlayerHitFeedback2D>() == null)
                throw new InvalidOperationException("Player blink feedback is missing.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            if (hud.GetComponent<PlayerDamageFeedbackUI>() == null)
                throw new InvalidOperationException("HUD damage feedback is missing.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
        ValidateDamageBehavior();
        EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        TrainingDummy2D[] dummies = UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
            FindObjectsSortMode.None);
        if (dummies.Length != 4)
            throw new InvalidOperationException("Test scene must have exactly four dummies.");
        foreach (TrainingDummy2D dummy in dummies)
        {
            bool infinite = dummy.name.Contains("South 5.1") || dummy.name.Contains("West 7.2");
            bool moving = dummy.name.Contains("East 4.1") || dummy.name.Contains("West 7.2");
            if (dummy.InfiniteHealth != infinite ||
                (dummy.GetComponent<TrainingDummyPatrol2D>() != null) != moving ||
                (dummy.transform.Find("Infinite Health Label") != null) != infinite)
                throw new InvalidOperationException("Dummy variant is incorrect: " + dummy.name);
        }
        EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Play scene contains a test dummy.");
        PrototypePlaySetup.Validate();
        BasicRangedEnemySetup.Validate();
        Debug.Log("Full damage reporting, 200/infinite dummy pairs, and damage feedback validated.");
    }

    private static void ValidateDamageBehavior()
    {
        GameObject finiteObject = new GameObject("Finite Dummy Damage Check");
        GameObject infiniteObject = new GameObject("Infinite Dummy Damage Check");
        GameObject playerObject = new GameObject("Player Invulnerability Check");
        try
        {
            TrainingDummy2D finite = finiteObject.AddComponent<TrainingDummy2D>();
            TrainingDummy2D infinite = infiniteObject.AddComponent<TrainingDummy2D>();
            typeof(TrainingDummy2D).GetMethod("Restore", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(finite, null);
            infinite.SetInfiniteHealth(true);
            float reported = 0f;
            Action<float, Vector3> listener = (value, _) => reported = value;
            CombatTarget2D.Damaged += listener;
            try
            {
                finite.TakeDamage(250f);
                if (reported != 250f || finite.IsAlive)
                    throw new InvalidOperationException("Finite dummy clipped overkill damage.");
                reported = 0f;
                infinite.TakeDamage(250f);
                if (reported != 250f || !infinite.IsAlive || infinite.CurrentHealth != 200f)
                    throw new InvalidOperationException("Infinite dummy did not retain health.");
            }
            finally
            {
                CombatTarget2D.Damaged -= listener;
            }

            PlayerStats stats = playerObject.AddComponent<PlayerStats>();
            typeof(PlayerStats).GetMethod("Awake", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(stats, null);
            if (!stats.ReceiveDamage(10f) || stats.CurrentHealth != 90f ||
                !stats.IsInvulnerable || stats.ReceiveDamage(10f) || stats.CurrentHealth != 90f)
                throw new InvalidOperationException("Damage did not start three-second invulnerability.");
            float until = new SerializedObject(stats).FindProperty("invulnerabilitySeconds").floatValue;
            if (until != 3f)
                throw new InvalidOperationException("Invulnerability duration must be three seconds.");

            PlayerAugments augments = playerObject.AddComponent<PlayerAugments>();
            PlayerAutomaticAugments2D skills = playerObject.AddComponent<PlayerAutomaticAugments2D>();
            if (playerObject.transform.Find("Shield Aura") == null)
                typeof(PlayerAutomaticAugments2D).GetMethod("Awake", BindingFlags.Instance |
                    BindingFlags.NonPublic).Invoke(skills, null);
            SpriteRenderer shieldAura = playerObject.transform.Find("Shield Aura")?.GetComponent<SpriteRenderer>();
            MethodInfo updateShieldVisual = typeof(PlayerAutomaticAugments2D).GetMethod(
                "UpdateShieldVisual", BindingFlags.Instance | BindingFlags.NonPublic);
            if (shieldAura == null || shieldAura.enabled || shieldAura.sprite == null ||
                updateShieldVisual == null)
                throw new InvalidOperationException("Shield aura was not created in the player.");
            augments.Apply(AugmentId.Shield);
            updateShieldVisual.Invoke(skills, null);
            if (!shieldAura.enabled)
                throw new InvalidOperationException("Shield aura did not appear after the augment was acquired.");
            typeof(PlayerStats).GetField("invulnerableUntil", BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(stats, Time.time - 1f);
            if (stats.ReceiveDamage(10f) || stats.CurrentHealth != 90f ||
                !stats.IsInvulnerable || skills.ShieldReady || shieldAura.enabled)
                throw new InvalidOperationException("Shield break did not start invulnerability.");
            typeof(PlayerAutomaticAugments2D).GetField("shieldReadyAt", BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(skills, Time.time - 1f);
            updateShieldVisual.Invoke(skills, null);
            if (!shieldAura.enabled)
                throw new InvalidOperationException("Shield aura did not return after its cooldown.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(finiteObject);
            UnityEngine.Object.DestroyImmediate(infiniteObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
