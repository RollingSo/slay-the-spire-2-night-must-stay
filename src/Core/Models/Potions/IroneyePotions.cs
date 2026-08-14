using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Potions;

public sealed class PoisonGrease : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<HiddenPoisonPower>(3m) };

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<HiddenPoisonPower>() };

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext,
        Creature target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<HiddenPoisonPower>(
            choiceContext,
            target,
            DynamicVars[nameof(HiddenPoisonPower)].BaseValue,
            Owner.Creature,
            null);
    }
}

public sealed class ThrownArrowPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<MarkPower>(1m) };

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext,
        Creature target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<MarkPower>(
            choiceContext,
            target,
            DynamicVars[nameof(MarkPower)].BaseValue,
            Owner.Creature,
            null);

        MarkPower mark = target.GetPower<MarkPower>();
        if (mark != null)
            await mark.TriggerAll(choiceContext, Owner.Creature, null);
    }
}

public sealed class PickledTurtleNeckMeat : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Approach>(true)
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>(true));

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext,
        Creature target)
    {
        CardModel approach = Owner.Creature.CombatState
            .CreateCard<Approach>(Owner);
        CardModel retreat = Owner.Creature.CombatState
            .CreateCard<Retreat>(Owner);
        CardCmd.Upgrade(approach);
        CardCmd.Upgrade(retreat);

        await CardPileCmd.AddGeneratedCardToCombat(
            approach,
            PileType.Hand,
            Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            retreat,
            PileType.Hand,
            Owner);
    }
}
