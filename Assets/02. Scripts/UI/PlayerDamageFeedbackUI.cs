using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public sealed class PlayerDamageFeedbackUI : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.75f;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.42f;
    [SerializeField, Min(0.1f)] private float deathFadeDuration = 1.8f;

    private const int TextureWidth = 256;
    private const int TextureHeight = 144;

    private PlayerStats stats;
    private RawImage flash;
    private RawImage lowHealthEdge;
    private RawImage deathFade;
    private Texture2D edgeTexture;
    private Texture2D deathTexture;
    private Color32[] deathPixels;
    private float[] edgeDepths;
    private float flashRemaining;
    private float deathElapsed;
    private float renderedDeathProgress = -1f;

    private void Awake()
    {
        stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
            stats.Damaged += OnDamaged;
        edgeTexture = CreateEdgeTexture();
        flash = CreateOverlay("Damage Red Edge Flash", edgeTexture);
        lowHealthEdge = CreateOverlay("Low Health Red Vignette", edgeTexture);
        deathTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
        deathTexture.filterMode = FilterMode.Bilinear;
        deathTexture.wrapMode = TextureWrapMode.Clamp;
        deathPixels = new Color32[TextureWidth * TextureHeight];
        deathFade = CreateOverlay("Death Red To Black Fade", deathTexture);
        deathFade.color = Color.white;
        RenderDeathFade(0f);
    }

    private void Update()
    {
        if (Time.timeScale > 0f)
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.deltaTime);
        if (flash != null)
            flash.color = new Color(0.95f, 0.08f, 0.1f,
                flashDuration > 0f ? 0.45f * flashRemaining / flashDuration : 0f);
        if (stats == null)
            return;

        float healthRatio = stats.MaxHealth > 0f
            ? Mathf.Clamp01(stats.CurrentHealth / stats.MaxHealth) : 1f;
        if (lowHealthEdge != null)
            lowHealthEdge.color = new Color(0.58f, 0.035f, 0.055f,
                LowHealthAlpha(healthRatio, lowHealthThreshold));

        if (healthRatio <= 0f)
        {
            deathElapsed = Mathf.Min(deathFadeDuration,
                deathElapsed + Time.unscaledDeltaTime);
            RenderDeathFade(deathElapsed / deathFadeDuration);
        }
        else if (deathElapsed > 0f)
        {
            deathElapsed = 0f;
            RenderDeathFade(0f);
        }
    }

    public static float LowHealthAlpha(float healthRatio, float threshold)
    {
        if (healthRatio >= threshold || threshold <= 0f)
            return 0f;
        float severity = Mathf.Clamp01((threshold - healthRatio) / 0.5f);
        return 0.78f * Mathf.Pow(severity, 0.75f);
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
        Texture2D texture = new Texture2D(TextureWidth, TextureHeight,
            TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[TextureWidth * TextureHeight];
        edgeDepths = new float[pixels.Length];
        for (int y = 0; y < TextureHeight; y++)
        for (int x = 0; x < TextureWidth; x++)
        {
            float horizontal = Mathf.Abs((x + 0.5f) / TextureWidth * 2f - 1f);
            float vertical = Mathf.Abs((y + 0.5f) / TextureHeight * 2f - 1f);
            float squareRadius = Mathf.Max(horizontal, vertical);
            float roundRadius = Mathf.Sqrt(horizontal * horizontal + vertical * vertical) /
                Mathf.Sqrt(2f);
            float radius = Mathf.Lerp(squareRadius, roundRadius, 0.22f);
            edgeDepths[y * TextureWidth + x] = 1f - radius;
            float vignette = SmoothRange(0.47f, 1f, radius);
            pixels[y * TextureWidth + x] = new Color32(255, 255, 255,
                (byte)Mathf.RoundToInt(255f * vignette * vignette));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private void RenderDeathFade(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (Mathf.Abs(progress - renderedDeathProgress) < 0.002f)
            return;
        renderedDeathProgress = progress;
        float front = Mathf.Lerp(-0.12f, 1.12f, Mathf.SmoothStep(0f, 1f, progress));
        for (int index = 0; index < deathPixels.Length; index++)
        {
            float depth = edgeDepths[index];
            float black = 1f - SmoothRange(front - 0.06f, front + 0.08f, depth);
            float rim = Mathf.Clamp01(1f - Mathf.Abs(depth - front) / 0.1f) *
                (1f - black);
            float alpha = Mathf.Max(black, rim * 0.82f);
            deathPixels[index] = new Color32(
                (byte)Mathf.RoundToInt(150f * rim),
                (byte)Mathf.RoundToInt(12f * rim),
                (byte)Mathf.RoundToInt(17f * rim),
                (byte)Mathf.RoundToInt(255f * alpha));
        }
        deathTexture.SetPixels32(deathPixels);
        deathTexture.Apply();
    }

    private static float SmoothRange(float start, float end, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.Damaged -= OnDamaged;
        if (edgeTexture != null)
            Destroy(edgeTexture);
        if (deathTexture != null)
            Destroy(deathTexture);
    }
}
