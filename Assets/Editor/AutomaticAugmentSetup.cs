using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AutomaticAugmentSetup
{
    private const string PlayerPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string DummyPath = "Assets/04. Prefabs/Targets/TrainingDummy.prefab";
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string ScenePath = "Assets/01. Scenes/Prototype_Test.unity";

    public static void Apply()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            PlayerAutomaticAugments2D automatic = player.GetComponent<PlayerAutomaticAugments2D>();
            if (automatic == null)
                automatic = player.AddComponent<PlayerAutomaticAugments2D>();
            SerializedObject settings = new SerializedObject(automatic);
            settings.FindProperty("slowDuration").floatValue = 7f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }

        GameObject dummy = PrefabUtility.LoadPrefabContents(DummyPath);
        try
        {
            if (dummy.GetComponent<EnemyStatus2D>() == null)
                dummy.AddComponent<EnemyStatus2D>();
            PrefabUtility.SaveAsPrefabAsset(dummy, DummyPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(dummy);
        }

        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Text countdown = hud.transform.Find("R Countdown").GetComponent<Text>();
            countdown.fontSize = 24;
            countdown.alignment = TextAnchor.MiddleCenter;
            countdown.rectTransform.anchoredPosition = new Vector2(30f, -211f);
            countdown.rectTransform.sizeDelta = new Vector2(90f, 50f);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }

        EditorSceneManager.OpenScene(ScenePath);
        TrainingDummy2D east = null;
        foreach (TrainingDummy2D target in UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
                     FindObjectsSortMode.None))
        {
            if (target.name == "Training Dummy East 4.1")
                east = target;
        }
        if (east == null)
            throw new InvalidOperationException("East test dummy is missing.");
        Rigidbody2D body = east.GetComponent<Rigidbody2D>();
        if (body == null)
            body = east.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (east.GetComponent<TrainingDummyPatrol2D>() == null)
            east.gameObject.AddComponent<TrainingDummyPatrol2D>();
        EditorSceneManager.MarkSceneDirty(east.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Shield, slow field, stun lightning, moving dummy, and decimal R cooldown are ready.");
    }

    private static void Validate()
    {
        PlayerAutomaticAugments2D skills = UnityEngine.Object.FindFirstObjectByType<PlayerAutomaticAugments2D>();
        TrainingDummyPatrol2D patrol = UnityEngine.Object.FindFirstObjectByType<TrainingDummyPatrol2D>();
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        if (skills == null || patrol == null || hud == null ||
            patrol.GetComponent<EnemyStatus2D>() == null)
            throw new InvalidOperationException("Automatic skills or moving test dummy is not connected.");
        if (!Mathf.Approximately(new SerializedObject(skills)
                .FindProperty("slowDuration").floatValue, 7f))
            throw new InvalidOperationException("Ice field should last seven seconds.");

        Text countdown = hud.transform.Find("R Countdown").GetComponent<Text>();
        if (countdown.fontSize != 24 || countdown.rectTransform.sizeDelta.x < 90f)
            throw new InvalidOperationException("The decimal R countdown does not fit its slot.");

        EnemyStatus2D status = patrol.GetComponent<EnemyStatus2D>();
        status.SetAuraSlow(true, 0.75f);
        if (!Mathf.Approximately(status.MovementMultiplier, 0.75f))
            throw new InvalidOperationException("The slow field does not reduce movement by 25%.");
        status.ApplyStun(1.5f);
        if (!Mathf.Approximately(status.MovementMultiplier, 0f))
            throw new InvalidOperationException("Stun does not stop target movement.");
        status.Clear();
        if (!Mathf.Approximately(status.MovementMultiplier, 1f))
            throw new InvalidOperationException("Movement does not recover after status clears.");
        Debug.Log("Slow 25% for seven seconds, stun stop, moving dummy, and decimal R HUD validated.");
    }
}
