using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TargetingAndPortraitSetup
{
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";

    public static void Apply()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform portrait = hud.transform.Find("Portrait Placeholder");
            Transform badge = hud.transform.Find("Level Badge Border");
            if (portrait == null || badge == null)
                throw new InvalidOperationException("Portrait or level badge is missing.");
            Sprite circle = portrait.GetComponent<Image>().sprite;
            if (circle == null)
                throw new InvalidOperationException("Portrait circle sprite is missing.");

            Transform existing = hud.transform.Find("Dodge Cooldown Overlay");
            GameObject element;
            if (existing != null)
                element = existing.gameObject;
            else
            {
                element = new GameObject("Dodge Cooldown Overlay", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                element.transform.SetParent(hud.transform, false);
            }

            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(29f, -29f);
            rect.sizeDelta = new Vector2(142f, 142f);
            Image overlay = element.GetComponent<Image>();
            overlay.sprite = circle;
            overlay.color = new Color(0f, 0f, 0f, 0.65f);
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = false;
            overlay.fillAmount = 0f;
            overlay.raycastTarget = false;
            overlay.enabled = false;

            element.transform.SetSiblingIndex(portrait.GetSiblingIndex() + 1);
            if (element.transform.GetSiblingIndex() >= badge.GetSiblingIndex())
                throw new InvalidOperationException("Dodge overlay would cover the level badge.");
            hud.GetComponent<PlayerHudUI>().ConfigureDodgeCooldown(overlay);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }

        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Portrait dodge cooldown and hold-to-aim barrage are ready.");
    }

    private static void Validate()
    {
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        RSkillCooldown barrage = UnityEngine.Object.FindFirstObjectByType<RSkillCooldown>();
        PlayerMovement2D movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement2D>();
        if (hud == null || barrage == null || movement == null)
            throw new InvalidOperationException("HUD, barrage, or dodge is missing.");

        Transform portrait = hud.transform.Find("Portrait Placeholder");
        Transform overlayTransform = hud.transform.Find("Dodge Cooldown Overlay");
        Transform badge = hud.transform.Find("Level Badge Border");
        Image overlay = overlayTransform != null ? overlayTransform.GetComponent<Image>() : null;
        if (overlay == null || overlay.type != Image.Type.Filled ||
            overlay.fillMethod != Image.FillMethod.Radial360 ||
            overlay.fillOrigin != (int)Image.Origin360.Top ||
            overlayTransform.GetSiblingIndex() <= portrait.GetSiblingIndex() ||
            overlayTransform.GetSiblingIndex() >= badge.GetSiblingIndex())
            throw new InvalidOperationException("Portrait cooldown overlay is configured incorrectly.");
        if (new SerializedObject(hud).FindProperty("dodgeCooldownOverlay").objectReferenceValue != overlay ||
            movement.DashCooldownSeconds != 10f)
            throw new InvalidOperationException("Dodge cooldown is not connected to the portrait.");

        TrainingDummy2D north = null;
        foreach (TrainingDummy2D dummy in UnityEngine.Object.FindObjectsByType<TrainingDummy2D>(
                     FindObjectsSortMode.None))
        {
            if (dummy.name == "Training Dummy North 2.4")
                north = dummy;
        }
        if (north == null || barrage.ManualAimPoint.HasValue ||
            barrage.FindNearestVisibleDummy() != north)
            throw new InvalidOperationException("Automatic nearest targeting is incorrect.");
        Vector2 emptyGroundPoint = new Vector2(7.4f, 3.3f);
        barrage.ApplyAimInput(true, emptyGroundPoint);
        if (!barrage.ManualAimPoint.HasValue || barrage.ManualAimPoint.Value != emptyGroundPoint)
            throw new InvalidOperationException("Held left button did not aim at the pointer.");
        Vector2 movedPointer = new Vector2(-4.2f, 1.7f);
        barrage.ApplyAimInput(true, movedPointer);
        if (!barrage.ManualAimPoint.HasValue || barrage.ManualAimPoint.Value != movedPointer)
            throw new InvalidOperationException("Held aim did not follow the pointer.");
        barrage.ApplyAimInput(false, null);
        if (barrage.ManualAimPoint.HasValue || barrage.FindNearestVisibleDummy() != north)
            throw new InvalidOperationException("Releasing left button did not restore nearest targeting.");

        Vector2 directionToNorth = ((Vector2)north.transform.position -
                                    (Vector2)barrage.transform.position).normalized;
        Physics2D.SyncTransforms();
        bool northCanBeHit = false;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(
                     barrage.transform.position + (Vector3)directionToNorth * 0.55f,
                     directionToNorth, 4f))
        {
            if (hit.collider.GetComponentInParent<TrainingDummy2D>() == north)
                northCanBeHit = true;
        }
        if (!northCanBeHit)
            throw new InvalidOperationException("Directional shot cannot hit the north dummy collider.");

        GameObject testProjectile = new GameObject("Directional Projectile Validation");
        try
        {
            Projectile2D projectile = testProjectile.AddComponent<Projectile2D>();
            projectile.InitializeDirection(Vector2.right, 15f, 16f);
            if (!projectile.IsDirectional)
                throw new InvalidOperationException("Manual-aim projectile is not directional.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testProjectile);
        }
        Debug.Log("Portrait cooldown, held aim, pointer movement, release fallback, and directional hit validated.");
    }
}
