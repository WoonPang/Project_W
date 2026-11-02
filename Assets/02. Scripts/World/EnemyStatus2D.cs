using UnityEngine;

public sealed class EnemyStatus2D : MonoBehaviour
{
    private float stunnedUntil;
    private bool auraSlowed;
    private float slowMultiplier = 1f;

    public bool IsSlowed => auraSlowed;
    public bool IsStunned => Time.time < stunnedUntil;
    public float MovementMultiplier => IsStunned ? 0f : IsSlowed ? slowMultiplier : 1f;

    public void SetAuraSlow(bool active, float multiplier = 0.5f)
    {
        auraSlowed = active;
        slowMultiplier = active ? Mathf.Clamp01(multiplier) : 1f;
    }

    public void ApplyStun(float duration)
    {
        if (duration > 0f)
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
    }

    public void Clear()
    {
        auraSlowed = false;
        stunnedUntil = 0f;
        slowMultiplier = 1f;
    }
}
