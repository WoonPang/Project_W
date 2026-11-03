using System;
using System.Collections.Generic;

public enum AugmentId
{
    BarrageBullets,
    BarrageDamage,
    BarrageCooldown,
    Shield,
    EnemySlow,
    EnemyStun,
    DashCooldown,
    MaxHealth,
    AttackDamage,
    MoveSpeed,
    AttackSpeed,
    EnemySlowRange,
    EnemyStunRange
}

public enum AugmentCategory
{
    Character,
    AutomaticSkill,
    OtherSkill,
    Stat
}

public sealed class AugmentDefinition
{
    public AugmentId Id { get; }
    public AugmentCategory Category { get; }
    public string Title { get; }
    public int MaxLevel { get; }
    private readonly string[] levelDescriptions;

    public AugmentDefinition(AugmentId id, AugmentCategory category, string title,
        int maxLevel, params string[] descriptions)
    {
        Id = id;
        Category = category;
        Title = title;
        MaxLevel = maxLevel;
        levelDescriptions = descriptions;
        if (descriptions.Length != maxLevel && maxLevel != int.MaxValue)
            throw new ArgumentException("Every limited augment level needs a description.");
    }

    public string DescriptionForNextLevel(int currentLevel)
    {
        int index = Math.Min(currentLevel, levelDescriptions.Length - 1);
        return levelDescriptions[index];
    }

    public string LabelForNextLevel(int currentLevel)
    {
        return MaxLevel != int.MaxValue && currentLevel + 1 == MaxLevel
            ? "Lv.MAX" : "Lv" + (currentLevel + 1);
    }
}

public static class AugmentCatalog
{
    public static readonly IReadOnlyList<AugmentDefinition> All = new[]
    {
        new AugmentDefinition(AugmentId.BarrageBullets, AugmentCategory.Character,
            "무차별 난사 탄환", 5,
            "탄환 +10발", "탄환 +10발", "탄환 +10발",
            "탄환 +10발", "탄환 +10발"),
        new AugmentDefinition(AugmentId.BarrageDamage, AugmentCategory.Character,
            "무차별 난사 피해", 5,
            "피해 배율 +0.1", "피해 배율 +0.1", "피해 배율 +0.1",
            "피해 배율 +0.1", "피해 배율 +0.1"),
        new AugmentDefinition(AugmentId.BarrageCooldown, AugmentCategory.Character,
            "무차별 난사 쿨타임", 5,
            "쿨타임 30초 → 27.5초", "쿨타임 27.5초 → 25초",
            "쿨타임 25초 → 22.5초", "쿨타임 22.5초 → 20초",
            "쿨타임 20초 → 15초"),
        new AugmentDefinition(AugmentId.Shield, AugmentCategory.AutomaticSkill,
            "보호막", 5,
            "피해 1회 방어\n쿨타임 30초",
            "쿨타임 30초 → 27.5초", "쿨타임 27.5초 → 25초",
            "쿨타임 25초 → 22.5초", "쿨타임 22.5초 → 20초"),
        new AugmentDefinition(AugmentId.EnemySlow, AugmentCategory.AutomaticSkill,
            "얼음지대", 5,
            "25% 둔화 · 7초 지속\n쿨타임 20초",
            "쿨타임 20초 → 19초", "쿨타임 19초 → 18초",
            "쿨타임 18초 → 17초", "쿨타임 17초 → 15초"),
        new AugmentDefinition(AugmentId.EnemyStun, AugmentCategory.AutomaticSkill,
            "뇌전", 5,
            "3초 뒤 시전 · 1.5초 기절\n쿨타임 15초",
            "쿨타임 15초 → 14초", "쿨타임 14초 → 13초",
            "쿨타임 13초 → 12초", "쿨타임 12초 → 10초"),
        new AugmentDefinition(AugmentId.DashCooldown, AugmentCategory.OtherSkill,
            "회피 쿨타임", 5,
            "1초 감소 · 9초", "총 2초 감소 · 8초", "총 3초 감소 · 7초",
            "총 4초 감소 · 6초", "총 5초 감소 · 5초"),
        new AugmentDefinition(AugmentId.MaxHealth, AugmentCategory.Stat,
            "최대 체력 증가", int.MaxValue, "최대 체력 +5\n현재 체력 +5"),
        new AugmentDefinition(AugmentId.AttackDamage, AugmentCategory.Stat,
            "공격력 증가", int.MaxValue, "공격력 +2.5"),
        new AugmentDefinition(AugmentId.MoveSpeed, AugmentCategory.Stat,
            "이동속도 증가", 5,
            "이동속도 +0.05", "이동속도 +0.05", "이동속도 +0.05",
            "이동속도 +0.05", "이동속도 +0.05"),
        new AugmentDefinition(AugmentId.AttackSpeed, AugmentCategory.Stat,
            "공격속도 증가", 10,
            "공격속도 +0.1", "공격속도 +0.1", "공격속도 +0.1",
            "공격속도 +0.1", "공격속도 +0.1", "공격속도 +0.1",
            "공격속도 +0.1", "공격속도 +0.1", "공격속도 +0.1",
            "공격속도 +0.1"),
        new AugmentDefinition(AugmentId.EnemySlowRange, AugmentCategory.AutomaticSkill,
            "얼음지대 범위 증가", 5,
            "범위 +10%", "범위 +10%", "범위 +10%",
            "범위 +10%", "범위 +10%"),
        new AugmentDefinition(AugmentId.EnemyStunRange, AugmentCategory.AutomaticSkill,
            "뇌전 범위 증가", 5,
            "범위 +10%", "범위 +10%", "범위 +10%",
            "범위 +10%", "범위 +10%")
    };

    public static AugmentDefinition Get(AugmentId id)
    {
        return All[(int)id];
    }

    public static float BarrageCooldownReduction(int level)
    {
        return level == 5 ? 15f : 2.5f * level;
    }

    public static float AutomaticSkillRangeMultiplier(int level)
    {
        return 1f + 0.1f * Math.Min(5, Math.Max(0, level));
    }

    public static float AutomaticSkillCooldown(AugmentId id, int level)
    {
        if (level <= 0)
            return 0f;
        if (id == AugmentId.Shield)
            return 30f - 2.5f * (level - 1);
        if (id == AugmentId.EnemySlow)
            return 20f - (level == 5 ? 5f : level - 1);
        if (id == AugmentId.EnemyStun)
            return 15f - (level == 5 ? 5f : level - 1);
        throw new ArgumentOutOfRangeException(nameof(id));
    }
}
