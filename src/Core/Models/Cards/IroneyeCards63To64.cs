using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Cards;

// Card-table ID 63: 迅捷
public sealed class IroneyeSwift : CardModel
{
    private const string CardsKey = "Cards";
    private const string DistanceKey = "Distance";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar(CardsKey, 1m),
            new DynamicVar(DistanceKey, 1m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/ironeye_swift.png");

    public IroneyeSwift()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CardPileCmd.Draw(context, DynamicVars[CardsKey].IntValue, Owner);

        decimal distanceDelta = cardPlay.Target == Owner.Creature
            ? DynamicVars[DistanceKey].BaseValue
            : -DynamicVars[DistanceKey].BaseValue;
        await PowerCmd.Apply<DistancePower>(
            context,
            Owner.Creature,
            distanceDelta,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

// Card-table ID 64: 凋零斩
public sealed class WitheringCut : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(3m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/withering_cut.png");

    public WitheringCut()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
            .Execute(context);

        decimal damageDealt = attack.Results
            .SelectMany(resultSet => resultSet)
            .Sum(result => result.TotalDamage);
        if (damageDealt > 0m && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<HiddenPoisonPower>(
                context,
                cardPlay.Target,
                damageDealt,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(2m);
}
