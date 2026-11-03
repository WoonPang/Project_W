using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public sealed class PlayerDamageFeedbackUI : MonoBehaviour
{
    [SerializeField, Range(0.05f, 1f)] private float lowHealthThreshold = 0.3f;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.42f;

    private PlayerStats stats;
    private RawImage flash;
    private RawImage lowHealthEdge;
    private Texture2D edgeTexture;
    private float flashRemaining;

    private void Awake()
    {
        stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
            stats.Damaged += OnDamaged;
        flash = CreateOverlay("Damage Red Flash", Texture2D.whiteTexture);
        lowHealthEdge = CreateOverlay("Low Health Red Edge", CreateEdgeTexture());
    }

    private void Update()
    {
        if (Time.timeScale > 0f)
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.deltaTime);
        if (flash != null)
            flash.color = new Color(0.85f, 0.08f, 0.1f,
                flashDuration > 0f ? 0.34f * flashRemaining / flashDuration : 0f);
        if (lowHealthEdge == null || stats == null)
            return;
        float healthRatio = stats.MaxHealth > 0f
            ? stats.CurrentHealth / stats.MaxHealth : 1f;
        if (healthRatio <= 0f || healthRatio > lowHealthThreshold)
        {
            lowHealthEdge.color = new Color(0.8f, 0.07f, 0.11f, 0f);
            return;
        }
        float severity = 1f - healthRatio / lowHealthThreshold;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        lowHealthEdge.color = new Color(0.8f, 0.07f, 0.11f,
            0.17f + 0.16f * severity + 0.09f * pulse);
    }

    private void OnDamaged(float _)
    {
        flashRemaining = flashDuration;
    }

    private RawImage CreateOverlay(string name, Texture texture)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        overlay.transform.SetParent(transform, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        RawImage image = overlay.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.clear;
        image.raycastTarget = false;
        return image;
    }

    private Texture2D CreateEdgeTexture()
    {
        const int width = 128;
        const int height = 72;
        edgeTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        edgeTexture.filterMode = FilterMode.Bilinear;
        edgeTexture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float horizontal = Mathf.Min(x, width - 1 - x) / (float)width;
            float vertical = Mathf.Min(y, height - 1 - y) / (float)height;
            float edge = 1f - Mathf.SmoothStep(0f, 0.14f, Mathf.Min(horizontal, vertical));
            pixels[y * width + x] = new Color32(255, 255, 255,
                (byte)Mathf.RoundToInt(255f * edge * edge));
        }
        edgeTexture.SetPixels32(pixels);
        edgeTexture.Apply();
        return edgeTexture;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.Damaged -= OnDamaged;
        if (edgeTexture != null)
            Destroy(edgeTexture);
    }
}
