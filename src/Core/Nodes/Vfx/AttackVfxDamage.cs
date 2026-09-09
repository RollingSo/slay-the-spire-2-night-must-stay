#nullable enable
using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Compatibility;

namespace NightMustStay.Core.Nodes.Vfx;

internal static class AttackVfxDamage
{
    // Query outgoing pre-block damage through the same read-only preview hooks
    // used by cards. Keep decimal precision; never narrow growing stacks to int.
    internal static decimal Preview(Creature target, Creature dealer, decimal amount, ValueProp props, CardModel? card = null)
    {
        if (dealer.CombatState == null) return Math.Max(0m, amount);
        return Math.Max(0m, Sts2BranchCompat.ModifyDamage(
            IRunState.GetFrom(new[] { dealer, target }), dealer.CombatState, target, dealer,
            amount, props, card, ModifyDamageHookType.All, CardPreviewMode.Normal, out _));
    }
}
