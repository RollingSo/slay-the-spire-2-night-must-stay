using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
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
    private readonly bool _powered;

    public override int Repeats => _repeats;

    public RevenantFamilyAttackIntent(int damage, int repeats = 1, bool powered = true)
    {
        _damage = Math.Max(0, damage);
        _repeats = Math.Max(1, repeats);
        _powered = powered;
        DamageCalc = () => _damage;
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner) =>
        GetDamage(owner) * _repeats;

    public static int CalculatePoweredDamage(Creature owner, int damage)
    {
        if (owner?.CombatState == null) return damage;
        return (int)Math.Max(0m, NightMustStay.Core.Compatibility.Sts2BranchCompat.ModifyDamage(
            owner.CombatState.RunState, owner.CombatState, null, owner, damage,
            ValueProp.Move, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _));
    }

    private int GetDamage(Creature owner) => _powered ? CalculatePoweredDamage(owner, _damage) : _damage;

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        LocString label = new("intents", _repeats > 1
            ? "FORMAT_DAMAGE_MULTI"
            : "FORMAT_DAMAGE_SINGLE");
        label.Add("Damage", GetDamage(owner));
        label.Add("Repeat", _repeats);
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        LocString description = new("intents", "ATTACK.description");
        description.Add(
            "IsMultiplayer",
            owner.CombatState?.RunState.Players.Count > 1);
        description.Add("Damage", GetDamage(owner));
        description.Add("Repeat", _repeats);
        return description;
    }
}
