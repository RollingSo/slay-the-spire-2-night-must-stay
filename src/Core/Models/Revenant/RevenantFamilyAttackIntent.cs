using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace NightMustStay.Core.Models.Revenant;

/// <summary>
/// Player-side family intents must not use AttackIntent's default preview
/// calculation, which treats the local player as the attack target and would
/// incorrectly apply the Revenant's Vulnerable to her family's damage label.
/// </summary>
public sealed class RevenantFamilyAttackIntent : AttackIntent
{
    private readonly int _damage;
    private readonly int _repeats;

    public override int Repeats => _repeats;

    public RevenantFamilyAttackIntent(int damage, int repeats = 1)
    {
        _damage = Math.Max(0, damage);
        _repeats = Math.Max(1, repeats);
        DamageCalc = () => _damage;
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner) =>
        _damage * _repeats;

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        LocString label = new("intents", _repeats > 1
            ? "FORMAT_DAMAGE_MULTI"
            : "FORMAT_DAMAGE_SINGLE");
        label.Add("Damage", _damage);
        label.Add("Repeat", _repeats);
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        LocString description = new("intents", "ATTACK.description");
        description.Add(
            "IsMultiplayer",
            owner.CombatState?.RunState.Players.Count > 1);
        description.Add("Damage", _damage);
        description.Add("Repeat", _repeats);
        return description;
    }
}
