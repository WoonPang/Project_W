using System;
using UnityEngine;

public sealed class TrainingDummy2D : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 200f;
    [SerializeField, Min(0f)] private float respawnDelay = 2.5f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer healthFillRenderer;

    [SerializeField] private float currentHealth;
    private float respawnAt;
    private Color originalBodyColor;
    private EnemyStatus2D status;

    public bool IsAlive => currentHealth > 0f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public static event Action<float, Vector3> Damaged;

    public void Configure(SpriteRenderer body, SpriteRenderer healthFill)
    {
        bodyRenderer = body;
        healthFillRenderer = healthFill;
    }

    private void Awake()
    {
        status = GetComponent<EnemyStatus2D>();
        if (bodyRenderer != null)
            originalBodyColor = bodyRenderer.color;
        Restore();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!IsAlive && Time.time >= respawnAt)
            Restore();

        if (bodyRenderer != null && IsAlive)
            bodyRenderer.color = status != null && status.IsStunned
                ? new Color(1f, 0.9f, 0.3f)
                : status != null && status.IsSlowed
                    ? new Color(0.48f, 0.9f, 1f)
                    : originalBodyColor;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        float actualDamage = Mathf.Min(currentHealth, amount);
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Damaged?.Invoke(actualDamage, transform.position + Vector3.up * 1.1f);
        UpdateHealthBar();

        if (!IsAlive)
        {
            respawnAt = Time.time + respawnDelay;
            if (bodyRenderer != null)
                bodyRenderer.color = Color.gray;
        }
    }

    private void Restore()
    {
        if (status != null)
            status.Clear();
        currentHealth = maxHealth;
        if (bodyRenderer != null)
            bodyRenderer.color = originalBodyColor;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillRenderer == null)
            return;

        float ratio = Mathf.Clamp01(currentHealth / maxHealth);
        Transform fill = healthFillRenderer.transform;
        fill.localScale = new Vector3(1.3f * ratio, 0.11f, 1f);
        fill.localPosition = new Vector3(-0.65f * (1f - ratio), 0.78f, -0.01f);
        healthFillRenderer.color = Color.Lerp(
            new Color(0.95f, 0.25f, 0.25f), new Color(0.28f, 0.9f, 0.42f), ratio);
    }
}
