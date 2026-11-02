using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerAugments))]
public sealed class PlayerAutomaticAugments2D : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float shieldRadius = 0.9f;
    [SerializeField, Min(0.5f)] private float slowRadius = 3f;
    [SerializeField, Min(0.1f)] private float slowDuration = 7f;
    [SerializeField, Min(0.5f)] private float stunRadius = 3f;
    [SerializeField, Min(0.1f)] private float stunDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float stunWindup = 3f;

    private readonly List<EnemyStatus2D> slowedTargets = new List<EnemyStatus2D>();
    private PlayerAugments augments;
    private SpriteRenderer shieldRing;
    private SpriteRenderer slowRing;
    private SpriteRenderer stunOutline;
    private SpriteRenderer stunChargeFill;
    private Texture2D ringTexture;
    private Sprite ringSprite;
    private Texture2D outlineTexture;
    private Sprite outlineSprite;
    private float shieldReadyAt;
    private float nextSlowAt;
    private float slowActiveUntil;
    private float nextStunAt;
    private float stunStrikeAt;
    private bool stunCharging;

    public bool ShieldReady => augments != null && augments.GetLevel(AugmentId.Shield) > 0 &&
                               Time.time >= shieldReadyAt;
    public bool SlowActive => Time.time < slowActiveUntil;
    public float SlowActiveRemaining => Mathf.Max(0f, slowActiveUntil - Time.time);
    public float SlowCooldownRemaining => Mathf.Max(0f, nextSlowAt - Time.time);
    public bool StunCharging => stunCharging;
    public float StunChargeRemaining => stunCharging ? Mathf.Max(0f, stunStrikeAt - Time.time) : 0f;
    public float StunCooldownRemaining => Mathf.Max(0f, nextStunAt - Time.time);
    public float ShieldCooldownRemaining => Mathf.Max(0f, shieldReadyAt - Time.time);

    private void Awake()
    {
        augments = GetComponent<PlayerAugments>();
        CreateCircleSprites();
        shieldRing = CreateRing("Shield Ready Ring", shieldRadius,
            new Color(0.12f, 0.4f, 1f, 0.9f), 12);
        slowRing = CreateRing("Slow Field Ring", slowRadius,
            new Color(0.3f, 0.9f, 1f, 0.75f), 8);
        stunOutline = CreateRing("Thunder Warning Outline", stunRadius,
            new Color(1f, 0.83f, 0.1f, 0.85f), 9, outlineSprite);
        stunChargeFill = CreateRing("Thunder Charge Fill", stunRadius,
            new Color(1f, 0.83f, 0.1f, 0.62f), 7);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (shieldRing != null)
        {
            shieldRing.enabled = ShieldReady;
            if (ShieldReady)
                shieldRing.color = new Color(0.12f, 0.4f, 1f,
                    0.72f + 0.18f * Mathf.Sin(Time.time * 4f));
        }

        UpdateSlow();
        UpdateStun();
        UpdateStunTelegraph();
    }

    public bool TryBlockHit()
    {
        if (!ShieldReady)
            return false;

        shieldReadyAt = Time.time + AugmentCatalog.AutomaticSkillCooldown(
            AugmentId.Shield, augments.GetLevel(AugmentId.Shield));
        if (shieldRing != null)
            shieldRing.enabled = false;
        return true;
    }

    private void UpdateSlow()
    {
        int level = augments.GetLevel(AugmentId.EnemySlow);
        if (level > 0 && Time.time >= nextSlowAt)
        {
            slowActiveUntil = Time.time + slowDuration;
            nextSlowAt = slowActiveUntil + AugmentCatalog.AutomaticSkillCooldown(
                AugmentId.EnemySlow, level);
        }

        foreach (EnemyStatus2D target in slowedTargets)
        {
            if (target != null)
                target.SetAuraSlow(false);
        }
        slowedTargets.Clear();

        bool active = level > 0 && SlowActive;
        slowRing.enabled = active;
        if (!active)
            return;

        float alpha = CalculateSlowFieldOpacity(SlowActiveRemaining);
        slowRing.color = new Color(0.3f, 0.9f, 1f, alpha);
        float radiusSquared = slowRadius * slowRadius;
        foreach (EnemyStatus2D target in FindObjectsByType<EnemyStatus2D>(FindObjectsSortMode.None))
        {
            if (!IsLiveTarget(target) ||
                (target.transform.position - transform.position).sqrMagnitude > radiusSquared)
                continue;
            target.SetAuraSlow(true, 0.75f);
            slowedTargets.Add(target);
        }
    }

    private float CalculateSlowFieldOpacity(float remaining)
    {
        if (remaining > 3f)
            return 0.75f;

        float cycles = remaining > 2f
            ? 0.8f * (3f - remaining)
            : remaining > 1f
                ? 0.8f + 2f * (2f - remaining)
                : 2.8f + 4f * (1f - remaining);
        float alpha = 0.18f + 0.57f *
            (0.5f + 0.5f * Mathf.Cos(cycles * Mathf.PI * 2f));
        if (remaining <= 1f)
            alpha *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(remaining));
        return alpha;
    }

    private void UpdateStun()
    {
        int level = augments.GetLevel(AugmentId.EnemyStun);
        if (level <= 0)
            return;

        if (stunCharging)
        {
            if (Time.time < stunStrikeAt)
                return;
            stunCharging = false;
            if (StrikeNearbyTargets())
                nextStunAt = Time.time + AugmentCatalog.AutomaticSkillCooldown(
                    AugmentId.EnemyStun, level);
            return;
        }

        if (Time.time < nextStunAt || !HasNearbyTarget())
            return;
        stunCharging = true;
        stunStrikeAt = Time.time + stunWindup;
    }

    private bool HasNearbyTarget()
    {
        float radiusSquared = stunRadius * stunRadius;
        foreach (EnemyStatus2D target in FindObjectsByType<EnemyStatus2D>(FindObjectsSortMode.None))
        {
            if (IsLiveTarget(target) &&
                (target.transform.position - transform.position).sqrMagnitude <= radiusSquared)
                return true;
        }
        return false;
    }

    private bool StrikeNearbyTargets()
    {
        bool hitAny = false;
        float radiusSquared = stunRadius * stunRadius;
        foreach (EnemyStatus2D target in FindObjectsByType<EnemyStatus2D>(FindObjectsSortMode.None))
        {
            if (!IsLiveTarget(target) ||
                (target.transform.position - transform.position).sqrMagnitude > radiusSquared)
                continue;
            target.ApplyStun(stunDuration);
            LightningBurst2D.Spawn(target.transform);
            hitAny = true;
        }
        return hitAny;
    }

    private void UpdateStunTelegraph()
    {
        stunOutline.enabled = stunCharging;
        stunChargeFill.enabled = stunCharging;
        if (!stunCharging)
            return;

        float progress = Mathf.Clamp01(1f - StunChargeRemaining / stunWindup);
        stunChargeFill.transform.localScale = Vector3.one *
                                              (stunRadius * 2f * Mathf.Max(0.02f, progress));
        stunChargeFill.color = new Color(1f, 0.83f, 0.1f, 0.62f);
    }

    private static bool IsLiveTarget(EnemyStatus2D status)
    {
        if (status == null || !status.isActiveAndEnabled)
            return false;
        TrainingDummy2D dummy = status.GetComponent<TrainingDummy2D>();
        return dummy == null || dummy.IsAlive;
    }

    private SpriteRenderer CreateRing(string name, float radius, Color color,
        int sortingOrder, Sprite sprite = null)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer renderer = ring.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : ringSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;
        return renderer;
    }

    private void CreateCircleSprites()
    {
        const int size = 128;
        ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[size * size];
        Color32[] outlinePixels = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / size;
            float fill = radius < 0.49f ? 0.55f : 0f;
            float edge = Mathf.Clamp01((radius - 0.455f) * 100f) *
                         Mathf.Clamp01((0.49f - radius) * 100f);
            float alpha = Mathf.Max(fill, edge);
            pixels[y * size + x] = new Color32(255, 255, 255,
                (byte)Mathf.RoundToInt(alpha * 255f));
            outlinePixels[y * size + x] = new Color32(255, 255, 255,
                (byte)Mathf.RoundToInt(edge * 255f));
        }
        ringTexture.SetPixels32(pixels);
        ringTexture.Apply(false, false);
        ringSprite = Sprite.Create(ringTexture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);

        outlineTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        outlineTexture.SetPixels32(outlinePixels);
        outlineTexture.Apply(false, false);
        outlineSprite = Sprite.Create(outlineTexture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    private void OnDestroy()
    {
        foreach (EnemyStatus2D target in slowedTargets)
        {
            if (target != null)
                target.SetAuraSlow(false);
        }
        if (ringSprite != null)
            Destroy(ringSprite);
        if (ringTexture != null)
            Destroy(ringTexture);
        if (outlineSprite != null)
            Destroy(outlineSprite);
        if (outlineTexture != null)
            Destroy(outlineTexture);
    }
}
