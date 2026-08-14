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
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Cards;

// Card-table ID 78: 攻势
public sealed class Offensive : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(4m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Approach>();

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/offensive.png");

    public Offensive()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
            .Execute(context);

        CardModel approach = CombatState.CreateCard<Approach>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(approach);
        await CardPileCmd.AddGeneratedCardToCombat(approach, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
    }
}

// Card-table ID 79: 回风箭
public sealed class ReturningWindArrow : CardModel, IMarkTriggerObserver
{
    private const int BaseDamage = 8;
    private const string GrowthKey = "Growth";
    private int _currentDamage = BaseDamage;
    private int _permanentDamage;
    private bool _triggeredMark;

    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            DynamicVars.Damage.BaseValue = value;
        }
    }

    [SavedProperty]
    public int PermanentDamage
    {
        get => _permanentDamage;
        set
        {
            AssertMutable();
            _permanentDamage = value;
            // Also update CurrentDamage for saves made before CurrentDamage was
            // serialized separately.  New saves restore the same value through
            // both properties regardless of deserialization order.
            UpdateDamage();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DamageVar(CurrentDamage, ValueProp.Move),
            new DynamicVar(GrowthKey, 3m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<MarkPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/returning_wind_arrow.png");

    public ReturningWindArrow()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public void OnMarkTriggered(decimal triggeringDamage) => _triggeredMark = true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        _triggeredMark = false;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(target =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
            .Execute(context);

        if (!_triggeredMark)
            return;

        int growth = DynamicVars[GrowthKey].IntValue;
        BuffFromPlay(growth);
        (DeckVersion as ReturningWindArrow)?.BuffFromPlay(growth);
    }

    private void BuffFromPlay(int amount)
    {
        PermanentDamage += amount;
    }

    protected override void OnUpgrade() =>
        DynamicVars[GrowthKey].UpgradeValueBy(2m);

    protected override void AfterDowngraded() => UpdateDamage();

    private void UpdateDamage()
    {
        CurrentDamage = BaseDamage + PermanentDamage;
    }
}

// Card-table ID 80: 折返步
public sealed class ReversalStep : CardModel
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new CalculationExtraVar(3m),
            new PostDistanceCalculatedBlockVar(
                ValueProp.Move,
                static (card, _) =>
                    2m * decimal.Abs(
                        card.Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m),
                static card =>
                    -(card.Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m)),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/reversal_step.png");

    public ReversalStep()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        DistancePower distance = Owner.Creature.GetPower<DistancePower>();
        decimal before = distance?.DistanceMovedThisTurn ?? 0m;
        decimal current = distance?.Amount ?? 0m;
        await PowerCmd.Apply<DistancePower>(
            context,
            Owner.Creature,
            -2m * current,
            Owner.Creature,
            this);

        decimal moved = (Owner.Creature.GetPower<DistancePower>()
            ?.DistanceMovedThisTurn ?? before) - before;
        decimal block = moved * DynamicVars.CalculationExtra.BaseValue;
        if (block > 0m)
            await CreatureCmd.GainBlock(
                Owner.Creature,
                block,
                DynamicVars.CalculatedBlock.Props,
                cardPlay);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

// Card-table ID 81: 回身一箭
public sealed class TurningArrow : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new CalculationBaseVar(6m),
            new ExtraDamageVar(2m),
            new PostDistanceCalculatedDamageVar(
                ValueProp.Move,
                static (card, _) =>
                    2m * decimal.Abs(
                        card.Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m),
                static card =>
                    -(card.Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m)),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<DistancePower>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/turning_arrow.png");

    public TurningArrow()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        DistancePower distance = Owner.Creature.GetPower<DistancePower>();
        decimal current = distance?.Amount ?? 0m;
        await PowerCmd.Apply<DistancePower>(
            context,
            Owner.Creature,
            -2m * current,
            Owner.Creature,
            this);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(target =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
            .Execute(context);
    }

    protected override void OnUpgrade() =>
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
}

// Card-table ID 82: 追魂连箭
public sealed class SoulChasingVolley : CardModel, IMarkTriggerObserver
{
    private const string FollowupDamageKey = "FollowupDamage";
    private bool _triggeredMark;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DamageVar(5m, ValueProp.Move),
            new RepeatVar(3),
            new DynamicVar(FollowupDamageKey, 5m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/soul_chasing_volley.png");

    public SoulChasingVolley()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public void OnMarkTriggered(decimal triggeringDamage) => _triggeredMark = true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        decimal hitDamage = DynamicVars.Damage.BaseValue;
        for (int i = 0; i < DynamicVars.Repeat.IntValue && cardPlay.Target.IsAlive; i++)
        {
            _triggeredMark = false;
            await DamageCmd.Attack(hitDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);
            if (_triggeredMark)
                hitDamage += DynamicVars[FollowupDamageKey].BaseValue;
        }
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}

// Card-table ID 83: 蚀尽
public sealed class CorrodeAll : CardModel
{
    private const string ThresholdKey = "Threshold";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DamageVar(5m, ValueProp.Move),
            new DynamicVar(ThresholdKey, 5m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/corrode_all.png");

    public CorrodeAll()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(target =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
            .Execute(context);

        int hiddenPoison = (int)(cardPlay.Target
            .GetPower<HiddenPoisonPower>()?.Amount ?? 0m);
        int bursts = 1 + hiddenPoison / DynamicVars[ThresholdKey].IntValue;
        for (int i = 0; i < bursts && cardPlay.Target.IsAlive; i++)
        {
            await PoisonBurstPower.Trigger(
                context,
                cardPlay.Target,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars[ThresholdKey].UpgradeValueBy(-1m);
}

// Card-table ID 84: 百谋
public sealed class HundredSchemes : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Approach>()
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>());

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/hundred_schemes.png");

    public HundredSchemes()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        CardModel selected = (await CardSelectCmd.FromHand(
                context,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                null,
                this))
            .FirstOrDefault();
        if (selected == null)
            return;

        CardModel replacement = selected.Type == CardType.Attack
            ? CombatState.CreateCard<Approach>(Owner)
            : CombatState.CreateCard<Retreat>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(replacement);
        await CardCmd.Transform(selected, replacement);
    }

    protected override void OnUpgrade()
    {
    }
}

// Card-table ID 85: 斩乱麻
public sealed class CutThroughChaos : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DamageVar(3m, ValueProp.Move),
            new CalculationBaseVar(1m),
            new CalculationExtraVar(1m),
            new CalculatedVar("CalculatedHits").WithMultiplier(
                static (card, _) =>
                    card.Owner.Creature.GetPower<DistancePower>()
                        ?.DistanceMovedThisTurn ?? 0m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/cut_through_chaos.png");

    public CutThroughChaos()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int hits = (int)((CalculatedVar)DynamicVars["CalculatedHits"])
            .Calculate(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(hits)
            .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
            .Execute(context);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 86: 风华刃舞
public sealed class GracefulBladeDance : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(5m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/graceful_blade_dance.png");

    public GracefulBladeDance()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
            .Execute(context);
    }

    public override async Task AfterCardPlayedLate(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.Card != this
            || Pile == null
            || Pile.Type == PileType.Hand
            || !Pile.Type.IsCombatPile())
        {
            return;
        }

        EnergyCost.AddThisTurn(1);
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
