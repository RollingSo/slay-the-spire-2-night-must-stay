using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards;

// Card-table ID 58: 封喉
public sealed class ThroatSeal : CardModel
{
    private const string HiddenPoisonKey = "HiddenPoison";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<HiddenPoisonPower>(HiddenPoisonKey, 2m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/throat_seal.png");

    public ThroatSeal()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
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

        await IroneyeHiddenPoison.BurstOnce(
            context,
            cardPlay.Target,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars[HiddenPoisonKey].UpgradeValueBy(2m);
}

// Card-table ID 59: 无处可躲
public sealed class NowhereToHide : CardModel
{
    private const string MarkKey = "Mark";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<NowhereToHidePower>(MarkKey, 2m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/nowhere_to_hide.png");

    public NowhereToHide()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<NowhereToHidePower>(
            context,
            Owner.Creature,
            DynamicVars[MarkKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

// Card-table ID 60: 穿杨一箭
public sealed class WillowPiercingArrow : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(20m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<MarkPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/willow_piercing_arrow.png");

    public WillowPiercingArrow()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .CompatFromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitVfxNode(target =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
            .Execute(context);

        foreach (Creature enemy in enemies.Where(enemy => enemy.IsAlive))
        {
            if (enemy.GetPower<MarkPower>() is { } mark)
                await mark.TriggerAll(context, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(6m);
}

// Card-table ID 61: 烈性毒药
public sealed class VolatilePoison : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<MarkPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/volatile_poison.png");

    public VolatilePoison()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<VolatilePoisonPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 62: 追踪箭
public sealed class TrackingArrow : CardModel, IMarkTriggerObserver
{
    private bool _triggeredMark;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(4m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/tracking_arrow.png");

    public TrackingArrow()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public void OnMarkTriggered(decimal triggeringDamage) =>
        _triggeredMark = true;

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        _triggeredMark = false;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .CompatFromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(target =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
            .Execute(context);
        DynamicVars.Damage.BaseValue += 1m;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.Card != this
            || !_triggeredMark
            || Pile == null
            || Pile.Type == PileType.Hand
            || !Pile.Type.IsCombatPile())
        {
            return;
        }

        _triggeredMark = false;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(3m);
}
