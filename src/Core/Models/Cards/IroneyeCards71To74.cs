using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards;

// Card-table ID 71: 破空
public sealed class Skybreaker : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Innate } : Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<SkybreakerPower>(),
            HoverTipFactory.FromPower<LongShotPower>(),
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/air_rending_arrow.png");

    public Skybreaker()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<SkybreakerPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

// Card-table ID 72: 致命刃芒
public sealed class FatalBladeEdge : CardModel
{
    private const string ThresholdKey = "Threshold";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar(ThresholdKey, 3m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<FatalBladeEdgePower>(),
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<NightMustStayMarkPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/fatal_blade_edge.png");

    public FatalBladeEdge()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<FatalBladeEdgePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[ThresholdKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars[ThresholdKey].UpgradeValueBy(-1m);
}

// Card-table ID 73: 解脱
public sealed class Release : CardModel
{
    private const string ThresholdKey = "Threshold";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? Array.Empty<CardKeyword>() : new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move),
            new DynamicVar(ThresholdKey, 2m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<NightMustStayMarkPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/release.png");

    public Release()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .CompatFromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
            .Execute(choiceContext);

        if (!cardPlay.Target.IsAlive
            || (Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m)
                > -DynamicVars[ThresholdKey].BaseValue)
        {
            return;
        }

        NightMustStayMarkPower mark = cardPlay.Target.GetPower<NightMustStayMarkPower>();
        if (mark != null)
            await mark.TriggerAll(choiceContext, Owner.Creature, this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

// Card-table ID 74: 紧急上弦
public sealed class EmergencyNocking : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new BlockVar(5m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.Static(StaticHoverTip.Block) };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/emergency_nocking.png");

    public EmergencyNocking()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        CardPile discardPile = PileType.Discard.GetPile(Owner);
        if (!discardPile.Cards.Any(static card =>
                card.Type == CardType.Attack && card.EnergyCost.GetResolved() == 0))
            return;

        CardModel selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                discardPile,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                static card =>
                    card.Type == CardType.Attack && card.EnergyCost.GetResolved() == 0))
            .FirstOrDefault();
        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Hand);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Block.UpgradeValueBy(3m);
}
