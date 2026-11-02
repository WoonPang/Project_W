using UnityEngine;
using UnityEngine.UI;

public sealed class DamagePopupUI : MonoBehaviour
{
    [SerializeField] private Text damageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0.1f)] private float lifetime = 0.8f;
    [SerializeField, Min(0f)] private float riseDistance = 95f;

    private RectTransform rect;
    private Vector2 startPosition;
    private float elapsed;

    public void Configure(Text label, CanvasGroup group)
    {
        damageText = label;
        canvasGroup = group;
    }

    public void Show(float damage, Vector2 position)
    {
        rect = GetComponent<RectTransform>();
        startPosition = position;
        rect.anchoredPosition = position;
        damageText.text = damage.ToString("0.#");
        rect.localScale = Vector3.one * 0.65f;
        canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (rect == null || Time.timeScale <= 0f)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        rect.anchoredPosition = startPosition + Vector2.up * (riseDistance * t);
        rect.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.35f, Mathf.Clamp01(t * 2f));
        canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
        if (t >= 1f)
            Destroy(gameObject);
    }
}
