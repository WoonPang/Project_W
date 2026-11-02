using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DashAndCooldownSetup
{
    private const string PlayerPath = "Assets/04. Prefabs/Characters/PrototypePlayer.prefab";
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";

    public static void Apply()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            PlayerMovement2D movement = player.GetComponent<PlayerMovement2D>();
            RSkillCooldown barrage = player.GetComponent<RSkillCooldown>();
            if (movement == null || barrage == null)
                throw new InvalidOperationException("Player movement or R skill is missing.");

            SerializedObject serialized = new SerializedObject(movement);
            serialized.FindProperty("dashDistance").floatValue = 2.5f;
            serialized.FindProperty("dashDuration").floatValue = 0.18f;
            serialized.FindProperty("dashCooldownSeconds").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }

        RUsageGaugeSetup.Apply();
        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Move-direction Space dodge and ten-second cooldown are ready.");
    }

    private static void Validate()
    {
        PlayerMovement2D movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement2D>();
        RSkillCooldown barrage = UnityEngine.Object.FindFirstObjectByType<RSkillCooldown>();
        if (movement == null || barrage == null ||
            !Mathf.Approximately(movement.DashDistance, 2.5f) ||
            !Mathf.Approximately(movement.DashDuration, 0.18f) ||
            !Mathf.Approximately(movement.DashCooldownSeconds, 10f) ||
            !Mathf.Approximately(barrage.FiringDuration, 5f) ||
            !Mathf.Approximately(barrage.CooldownSeconds, 30f))
            throw new InvalidOperationException("Dash or barrage values are incorrect in Prototype.");
        Debug.Log("Dodge distance, duration, cooldown, and barrage timing validated.");
    }
}
