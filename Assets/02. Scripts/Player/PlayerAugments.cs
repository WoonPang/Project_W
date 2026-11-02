using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerAugments : MonoBehaviour
{
    private readonly int[] levels = new int[AugmentCatalog.All.Count];
    private PlayerStats stats;
    private RSkillCooldown barrage;
    private PlayerMovement2D movement;

    public event Action<AugmentId, int> AugmentApplied;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        barrage = GetComponent<RSkillCooldown>();
        movement = GetComponent<PlayerMovement2D>();
    }

    public int GetLevel(AugmentId id)
    {
        return levels[(int)id];
    }

    public bool CanUpgrade(AugmentId id)
    {
        return GetLevel(id) < AugmentCatalog.Get(id).MaxLevel;
    }

    public AugmentId[] RollOffers()
    {
        var eligible = new List<AugmentId>();
        bool coreUpgradeAvailable = false;
        foreach (AugmentDefinition definition in AugmentCatalog.All)
        {
            if (!CanUpgrade(definition.Id))
                continue;
            eligible.Add(definition.Id);
            if (definition.Category == AugmentCategory.Character ||
                definition.Category == AugmentCategory.AutomaticSkill)
                coreUpgradeAvailable = true;
        }

        if (eligible.Count < 3)
            throw new InvalidOperationException("At least three augments must remain available.");

        for (int index = eligible.Count - 1; index > 0; index--)
        {
            int other = UnityEngine.Random.Range(0, index + 1);
            (eligible[index], eligible[other]) = (eligible[other], eligible[index]);
        }

        if (coreUpgradeAvailable && IsStat(eligible[0]) && IsStat(eligible[1]) &&
            IsStat(eligible[2]))
        {
            int nonStat = eligible.FindIndex(3, id => !IsStat(id));
            if (nonStat < 0)
                throw new InvalidOperationException("A non-stat augment was expected.");
            int replacement = UnityEngine.Random.Range(0, 3);
            (eligible[replacement], eligible[nonStat]) =
                (eligible[nonStat], eligible[replacement]);
        }

        return new[] { eligible[0], eligible[1], eligible[2] };
    }

    public void Apply(AugmentId id)
    {
        if (!CanUpgrade(id))
            throw new InvalidOperationException("This augment has reached its maximum level.");

        if (stats == null)
            stats = GetComponent<PlayerStats>();
        if (barrage == null)
            barrage = GetComponent<RSkillCooldown>();
        if (movement == null)
            movement = GetComponent<PlayerMovement2D>();

        levels[(int)id]++;
        switch (id)
        {
            case AugmentId.BarrageBullets:
            case AugmentId.BarrageDamage:
            case AugmentId.BarrageCooldown:
                if (barrage != null)
                    barrage.ApplyAugmentLevels(GetLevel(AugmentId.BarrageBullets),
                        GetLevel(AugmentId.BarrageDamage), GetLevel(AugmentId.BarrageCooldown));
                break;
            case AugmentId.DashCooldown:
                if (movement != null)
                    movement.SetDashCooldownReduction(GetLevel(id));
                break;
            case AugmentId.MaxHealth:
                stats.AddMaxHealth(20f);
                break;
            case AugmentId.AttackDamage:
                stats.AddAttackDamage(10f);
                break;
            case AugmentId.MoveSpeed:
                stats.AddMoveSpeed(0.1f);
                break;
            case AugmentId.AttackSpeed:
                stats.AddAttackSpeed(0.1f);
                break;
            // Shield, slow, and stun read their current levels from this component.
        }
        AugmentApplied?.Invoke(id, levels[(int)id]);
    }

    private static bool IsStat(AugmentId id)
    {
        return AugmentCatalog.Get(id).Category == AugmentCategory.Stat;
    }
}
