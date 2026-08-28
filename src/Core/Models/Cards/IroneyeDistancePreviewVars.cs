using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

/// <summary>
/// Calculated damage whose card effect changes Distance before dealing damage.
/// The normal card preview runs hooks against the current Strength.  Runtime,
/// however, runs those hooks after Distance has changed Strength.  This var
/// shifts the preview's raw damage by the pending Strength delta so the same
/// global hooks produce the exact post-move result.
/// </summary>
internal sealed class PostDistanceCalculatedDamageVar : CalculatedDamageVar
{
    private readonly Func<CardModel, decimal> _futureDistance;

    public PostDistanceCalculatedDamageVar(
        ValueProp props,
        Func<CardModel, Creature, decimal> multiplier,
        Func<CardModel, decimal> futureDistance)
        : base(props)
    {
        _futureDistance = futureDistance;
        WithMultiplier(multiplier);
    }

    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature target,
        bool runGlobalHooks)
    {
        if (!runGlobalHooks || card.CombatState == null)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            return;
        }

        EnchantmentModel enchantment = card.Enchantment;
        if (enchantment != null)
        {
            decimal enchantedBase = GetBaseVar().BaseValue;
            enchantedBase += enchantment.EnchantDamageAdditive(
                enchantedBase,
                Props);
            enchantedBase *= enchantment.EnchantDamageMultiplicative(
                enchantedBase,
                Props);
            enchantedBase = Math.Max(enchantedBase, 0m);
            if (card.IsEnchantmentPreview)
                PreviewValue = enchantedBase;
            else
                EnchantedValue = enchantedBase;
        }

        decimal currentDistance =
            card.Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m;
        decimal futureDistance = decimal.Clamp(
            _futureDistance(card),
            DistancePower.MinimumDistance,
            DistancePower.MaximumDistance);
        decimal strengthDelta =
            GetDistanceStrengthContribution(card, futureDistance)
            - GetDistanceStrengthContribution(card, currentDistance);

        decimal rawDamage = Calculate(target) + strengthDelta;
        PreviewValue = Math.Max(
            Hook.ModifyDamage(
                card.Owner.RunState,
                card.CombatState,
                target,
                card.Owner.Creature,
                rawDamage,
                Props,
                card,
                ModifyDamageHookType.All,
                previewMode,
                out IEnumerable<AbstractModel> _),
            0m);
    }

    private static decimal GetDistanceStrengthContribution(
        CardModel card,
        decimal distance)
    {
        if (distance > 0m)
            return card.Owner.Creature.HasPower<BowLikeFullMoonPower>()
                ? 0m
                : -distance;

        if (distance < 0m)
        {
            decimal multiplier =
                card.Owner.Creature.HasPower<BladeShadowUnmatchedPower>()
                    ? 2m
                    : 1m;
            return -distance * multiplier;
        }

        return 0m;
    }
}

/// <summary>
/// Block counterpart to <see cref="PostDistanceCalculatedDamageVar"/>.
/// Distance contributes one Dexterity per point, so shifting the preview's raw
/// block by the pending Distance delta makes Frail and other multiplicative
/// block hooks resolve exactly as they will after the move.
/// </summary>
internal sealed class PostDistanceCalculatedBlockVar : CalculatedBlockVar
{
    private readonly Func<CardModel, decimal> _futureDistance;

    public PostDistanceCalculatedBlockVar(
        ValueProp props,
        Func<CardModel, Creature, decimal> multiplier,
        Func<CardModel, decimal> futureDistance)
        : base(props)
    {
        _futureDistance = futureDistance;
        WithMultiplier(multiplier);
    }

    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature target,
        bool runGlobalHooks)
    {
        if (!runGlobalHooks || card.CombatState == null)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            return;
        }

        EnchantmentModel enchantment = card.Enchantment;
        if (enchantment != null)
        {
            decimal enchantedBase = GetBaseVar().BaseValue;
            enchantedBase += enchantment.EnchantBlockAdditive(enchantedBase);
            enchantedBase *= enchantment.EnchantBlockMultiplicative(
                enchantedBase);
            if (card.IsEnchantmentPreview)
                PreviewValue = enchantedBase;
            else
                EnchantedValue = enchantedBase;
        }

        decimal currentDistance =
            card.Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m;
        decimal futureDistance = decimal.Clamp(
            _futureDistance(card),
            DistancePower.MinimumDistance,
            DistancePower.MaximumDistance);
        decimal dexterityDelta = futureDistance - currentDistance;
        decimal rawBlock = Calculate(target) + dexterityDelta;

        PreviewValue = Hook.ModifyBlock(
            card.CombatState,
            card.Owner.Creature,
            rawBlock,
            Props,
            card,
            null,
            out IEnumerable<AbstractModel> _);
    }
}
