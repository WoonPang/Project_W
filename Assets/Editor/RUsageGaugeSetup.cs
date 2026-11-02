using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RUsageGaugeSetup
{
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";
    private const string WhitePath = "Assets/03. Asset/Sprites/WhitePixel.png";

    public static void Apply()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
            Transform cooldown = hud.transform.Find("R Cooldown Overlay");
            Transform countdown = hud.transform.Find("R Countdown");
            if (white == null || cooldown == null || countdown == null)
                throw new InvalidOperationException("R slot assets are missing.");

            Transform existing = hud.transform.Find("R Usage Gauge");
            GameObject element;
            if (existing != null)
                element = existing.gameObject;
            else
            {
                element = new GameObject("R Usage Gauge", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                element.transform.SetParent(hud.transform, false);
            }

            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(27f, -187f);
            rect.sizeDelta = new Vector2(96f, 96f);
            Image gauge = element.GetComponent<Image>();
            gauge.sprite = white;
            gauge.color = new Color(0f, 0f, 0f, 0.45f);
            gauge.type = Image.Type.Filled;
            gauge.fillMethod = Image.FillMethod.Vertical;
            gauge.fillOrigin = (int)Image.OriginVertical.Bottom;
            gauge.fillAmount = 0f;
            gauge.raycastTarget = false;
            gauge.enabled = false;

            element.transform.SetSiblingIndex(cooldown.GetSiblingIndex() + 1);
            if (element.transform.GetSiblingIndex() >= countdown.GetSiblingIndex())
                throw new InvalidOperationException("R usage gauge would cover the countdown text.");
            hud.GetComponent<PlayerHudUI>().ConfigureUsageGauge(gauge);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }

        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("R usage gauge created with top-to-bottom drain.");
    }

    private static void Validate()
    {
        PlayerHudUI hud = UnityEngine.Object.FindFirstObjectByType<PlayerHudUI>();
        RSkillCooldown skill = UnityEngine.Object.FindFirstObjectByType<RSkillCooldown>();
        if (hud == null || skill == null || skill.FiringDuration != 5f)
            throw new InvalidOperationException("HUD or five-second R barrage is missing.");

        Transform gaugeTransform = hud.transform.Find("R Usage Gauge");
        Transform cooldown = hud.transform.Find("R Cooldown Overlay");
        Transform countdown = hud.transform.Find("R Countdown");
        Image gauge = gaugeTransform != null ? gaugeTransform.GetComponent<Image>() : null;
        if (gauge == null || gauge.type != Image.Type.Filled ||
            gauge.color != new Color(0f, 0f, 0f, 0.45f) ||
            gauge.fillMethod != Image.FillMethod.Vertical ||
            gauge.fillOrigin != (int)Image.OriginVertical.Bottom ||
            gaugeTransform.GetSiblingIndex() <= cooldown.GetSiblingIndex() ||
            gaugeTransform.GetSiblingIndex() >= countdown.GetSiblingIndex())
            throw new InvalidOperationException("R usage gauge fill or draw order is incorrect.");

        SerializedObject serialized = new SerializedObject(hud);
        if (serialized.FindProperty("rUsageGauge").objectReferenceValue != gauge)
            throw new InvalidOperationException("R usage gauge is not connected to HUD logic.");
        Debug.Log("R gauge fill direction, HUD reference, and firing duration validated.");
    }
}
