using UnityEngine;

// Clears surviving enemies after the final boss dies without granting rewards.
public sealed class EnemyClearFade2D : MonoBehaviour
{
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private float duration;
    private float elapsed;

    public void Begin(float seconds)
    {
        duration = Mathf.Max(0.05f, seconds);
        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
    }

    private void Update()
    {
        if (renderers == null)
            return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float opacity = 1f - Mathf.SmoothStep(0f, 1f, progress);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            Color color = originalColors[i];
            color.a *= opacity;
            renderers[i].color = color;
        }
        transform.localScale = originalScale * (1f - 0.12f * progress);
        if (progress >= 1f)
            Destroy(gameObject);
    }
}
