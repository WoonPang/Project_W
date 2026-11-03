using UnityEngine;

public enum EnemyHealthKind
{
    BasicMelee,
    TankMelee,
    BasicRanged,
    AssassinMelee,
    ChargingRanged,
    MidBoss,
    FinalBoss
}

public interface ILevelScaledEnemyHealth
{
    void ApplyLevelHealth(int level);
}

public static class EnemyHealthScaling2D
{
    public static float BaselineAttackDamage(int level)
    {
        return 10f + 2.5f * (Mathf.Max(1, level) - 1);
    }

    public static float MaxHealth(EnemyHealthKind kind, int level)
    {
        float damage = BaselineAttackDamage(level);
        switch (kind)
        {
            case EnemyHealthKind.BasicMelee:
                return damage * 2f;
            case EnemyHealthKind.TankMelee:
                return damage * 3.25f;
            case EnemyHealthKind.BasicRanged:
                return damage * 1.5f;
            case EnemyHealthKind.MidBoss:
                return damage * 15f;
            case EnemyHealthKind.FinalBoss:
                return damage * 50f;
            default:
                return damage;
        }
    }

    public static int CurrentPlayerLevel()
    {
        PlayerProgress player = Object.FindFirstObjectByType<PlayerProgress>();
        return player != null ? Mathf.Max(1, player.CurrentLevel) : 1;
    }

    public static void SyncExistingEnemies(int level)
    {
        foreach (CombatTarget2D target in Object.FindObjectsByType<CombatTarget2D>(
                     FindObjectsSortMode.None))
        {
            if (target.IsAlive && target is ILevelScaledEnemyHealth scaled)
                scaled.ApplyLevelHealth(level);
        }
    }

    public static float ScaleRemainingHealth(float oldCurrent, float oldMaximum,
        float newMaximum)
    {
        if (oldCurrent <= 0f)
            return 0f;
        float fraction = oldMaximum > 0f ? Mathf.Clamp01(oldCurrent / oldMaximum) : 1f;
        return newMaximum * fraction;
    }
}
