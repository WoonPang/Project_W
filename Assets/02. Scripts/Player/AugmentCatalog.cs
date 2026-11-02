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
    AttackSpeed
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
}

public static class AugmentCatalog
{
    public static readonly IReadOnlyList<AugmentDefinition> All = new[]
    {
        new AugmentDefinition(AugmentId.BarrageBullets, AugmentCategory.Character,
            "무차별 난사 탄환", 5,
            "+10발 · 총 60발", "+10발 · 총 70발", "+10발 · 총 80발",
            "+10발 · 총 90발", "+10발 · 총 100발"),
        new AugmentDefinition(AugmentId.BarrageDamage, AugmentCategory.Character,
            "무차별 난사 피해", 5,
            "배율 +0.1 · 총 1.6배", "배율 +0.1 · 총 1.7배",
            "배율 +0.1 · 총 1.8배", "배율 +0.1 · 총 1.9배",
            "배율 +0.1 · 총 2.0배"),
        new AugmentDefinition(AugmentId.BarrageCooldown, AugmentCategory.Character,
            "무차별 난사 쿨타임", 5,
            "2.5초 감소 · 27.5초", "총 5초 감소 · 25초",
            "총 7.5초 감소 · 22.5초", "총 10초 감소 · 20초",
            "총 15초 감소 · 15초"),
        new AugmentDefinition(AugmentId.Shield, AugmentCategory.AutomaticSkill,
            "공격 1회 보호", 5,
            "파란 보호막 원\n쿨타임 30초", "쿨타임 27.5초", "쿨타임 25초",
            "쿨타임 22.5초", "쿨타임 20초"),
        new AugmentDefinition(AugmentId.EnemySlow, AugmentCategory.AutomaticSkill,
            "얼음지대", 5,
            "25% 둔화 · 7초 지속\n반경 3 · 쿨타임 20초", "쿨타임 19초", "쿨타임 18초",
            "쿨타임 17초", "쿨타임 15초"),
        new AugmentDefinition(AugmentId.EnemyStun, AugmentCategory.AutomaticSkill,
            "뇌전", 5,
            "3초 후 1.5초 기절\n반경 3 · 쿨타임 15초", "쿨타임 14초", "쿨타임 13초",
            "쿨타임 12초", "쿨타임 10초"),
        new AugmentDefinition(AugmentId.DashCooldown, AugmentCategory.OtherSkill,
            "회피 쿨타임", 5,
            "1초 감소 · 9초", "총 2초 감소 · 8초", "총 3초 감소 · 7초",
            "총 4초 감소 · 6초", "총 5초 감소 · 5초"),
        new AugmentDefinition(AugmentId.MaxHealth, AugmentCategory.Stat,
            "최대 체력 증가", int.MaxValue, "최대 체력 +20\n현재 체력 +20"),
        new AugmentDefinition(AugmentId.AttackDamage, AugmentCategory.Stat,
            "공격력 증가", int.MaxValue, "공격력 +10"),
        new AugmentDefinition(AugmentId.MoveSpeed, AugmentCategory.Stat,
            "이동속도 증가", int.MaxValue, "이동속도 +0.1"),
        new AugmentDefinition(AugmentId.AttackSpeed, AugmentCategory.Stat,
            "공격속도 증가", int.MaxValue, "공격속도 +0.1")
    };

    public static AugmentDefinition Get(AugmentId id)
    {
        return All[(int)id];
    }

    public static float BarrageCooldownReduction(int level)
    {
        return level == 5 ? 15f : 2.5f * level;
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
