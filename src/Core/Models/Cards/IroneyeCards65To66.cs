using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

// Card-table ID 65: 幸运箭袋
public sealed class LuckyArrowBag : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded
            ? new[] { CardKeyword.Exhaust, CardKeyword.Retain }
            : new[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/lucky_arrow_bag.png");

    public LuckyArrowBag()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        while (PileType.Hand.GetPile(Owner).Cards.Count < CardPile.MaxCardsInHand)
        {
            CardModel drawn = await CardPileCmd.Draw(context, Owner);
            if (drawn == null || drawn.Type == CardType.Attack)
                break;
        }
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

// Card-table ID 66: 毒计
public sealed class PoisonScheme : CardModel
{
    private const string HiddenPoisonKey = "HiddenPoison";
    private const string RetainCardsKey = "RetainCards";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new PowerVar<HiddenPoisonPower>(HiddenPoisonKey, 3m),
            new PowerVar<PoisonSchemePower>(RetainCardsKey, 2m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/poison_scheme.png");

    public PoisonScheme()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<HiddenPoisonPower>(
            context,
            cardPlay.Target,
            DynamicVars[HiddenPoisonKey].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<PoisonSchemePower>(
            context,
            Owner.Creature,
            DynamicVars[RetainCardsKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars[RetainCardsKey].UpgradeValueBy(1m);
}
