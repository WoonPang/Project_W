using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ExperiencePickupSetup
{
    private const string HudPath = "Assets/04. Prefabs/UI/PlayerHUD.prefab";
    private const string PickupPath = "Assets/04. Prefabs/Items/ExperiencePickup.prefab";
    private const string CirclePath = "Assets/03. Asset/Sprites/HudCircle.png";

    public static void Apply()
    {
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
        if (circle == null)
            throw new InvalidOperationException("Experience pickup circle sprite is missing.");

        ExperiencePickup2D pickup = CreatePickupPrefab(circle);
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Text experienceText = hud.transform.Find("EXP Text").GetComponent<Text>();
            experienceText.text = "EXP 0%";
            experienceText.fontSize = 32;
            experienceText.resizeTextForBestFit = false;
            experienceText.rectTransform.sizeDelta = new Vector2(340f, 46f);

            Transform levelBorder = hud.transform.Find("레벨업 +1 Border");
            if (levelBorder == null)
                levelBorder = hud.transform.Find("레벨 +1 Border");
            if (levelBorder == null)
                throw new InvalidOperationException("Level test button is missing from HUD.");

            levelBorder.name = "레벨 +1 Border";
            Transform levelButton = levelBorder.GetChild(0);
            levelButton.name = "레벨 +1";
            levelButton.Find("Label").GetComponent<Text>().text = "레벨 +1";

            Transform spawnBorder = hud.transform.Find("경험치 더미 생성 Border");
            if (spawnBorder == null)
            {
                GameObject copy = UnityEngine.Object.Instantiate(levelBorder.gameObject, hud.transform);
                copy.name = "경험치 더미 생성 Border";
                spawnBorder = copy.transform;
            }

            RectTransform spawnRect = spawnBorder.GetComponent<RectTransform>();
            spawnRect.anchoredPosition = new Vector2(-25f, -265f);
            Transform spawnFace = spawnBorder.GetChild(0);
            spawnFace.name = "경험치 더미 생성";
            Text spawnLabel = spawnFace.Find("Label").GetComponent<Text>();
            spawnLabel.text = "경험치 더미 생성";
            spawnLabel.fontSize = 18;

            ExperiencePickupSpawner spawner = hud.GetComponent<ExperiencePickupSpawner>();
            if (spawner == null)
                spawner = hud.AddComponent<ExperiencePickupSpawner>();
            spawner.Configure(spawnFace.GetComponent<Button>(), pickup);

            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("EXP percent HUD and pickup dummy button are ready.");
    }

    private static ExperiencePickup2D CreatePickupPrefab(Sprite circle)
    {
        GameObject pickupObject = new GameObject("ExperiencePickup", typeof(SpriteRenderer),
            typeof(ExperiencePickup2D));
        try
        {
            pickupObject.transform.localScale = Vector3.one * 0.38f;
            SpriteRenderer renderer = pickupObject.GetComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = new Color(0.59f, 1f, 0.42f, 1f);
            renderer.sortingOrder = 20;

            string folder = "Assets/04. Prefabs/Items";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/04. Prefabs", "Items");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(pickupObject, PickupPath);
            return saved.GetComponent<ExperiencePickup2D>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(pickupObject);
        }
    }
}
