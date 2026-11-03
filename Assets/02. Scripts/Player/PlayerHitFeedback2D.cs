using UnityEngine;

[RequireComponent(typeof(PlayerStats), typeof(SpriteRenderer))]
public sealed class PlayerHitFeedback2D : MonoBehaviour
{
    private PlayerStats stats;
    private SpriteRenderer body;
    private Color normalColor;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        body = GetComponent<SpriteRenderer>();
        normalColor = body.color;
    }

    private void LateUpdate()
    {
        if (stats == null || body == null)
            return;
        Color color = normalColor;
        if (stats.IsInvulnerable)
            color.a *= Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? 0.28f : 1f;
        body.color = color;
    }

    private void OnDisable()
    {
        if (body != null)
            body.color = normalColor;
    }
}
