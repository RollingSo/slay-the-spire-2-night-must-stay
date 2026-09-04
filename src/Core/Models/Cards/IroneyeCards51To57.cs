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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

// Card-table ID 51: 死亡标记（欧洛巴斯的古老牙齿将“标记”转化为此牌）
public sealed class DeathMark : CardModel
{
    private const string MarkKey = "Mark";
    private const string DistanceKey = "Distance";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new BlockVar(8m, ValueProp.Move),
            new PowerVar<NightMustStayMarkPower>(MarkKey, 3m),
            new DynamicVar(DistanceKey, 1m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<NightMustStayMarkPower>(),
            HoverTipFactory.FromPower<DistancePower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/death_mark.png");

    public DeathMark()
        : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        await PowerCmd.Apply<NightMustStayMarkPower>(
            context,
            enemies,
            DynamicVars[MarkKey].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<DistancePower>(
            context,
            Owner.Creature,
            -DynamicVars[DistanceKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MarkKey].UpgradeValueBy(1m);
    }
}

// Card-table ID 52: 终局一战（达弗的先古魔典给予）
public sealed class FinalBattle : CardModel
{
    private const string DistanceKey = "Distance";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Retain };

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new BlockVar(99m, ValueProp.Move),
            new DynamicVar(DistanceKey, 5m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<FinalBattleNoBlockPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/final_battle.png");

    public FinalBattle()
        : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // The one-time Block must resolve before the permanent no-Block rule.
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<DistancePower>(
            context,
            Owner.Creature,
            -DynamicVars[DistanceKey].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<FinalBattleNoBlockPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 53: 狩猎序幕
public sealed class HuntingPrelude : CardModel
{
    private const string EnergyKey = "Energy";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new EnergyVar(EnergyKey, 1) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromCard<Approach>(IsUpgraded),
            HoverTipFactory.FromCard<Retreat>(IsUpgraded),
            EnergyHoverTip,
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/hunting_prelude.png");

    public HuntingPrelude()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        CardModel approach = CombatState.CreateCard<Approach>(Owner);
        CardModel retreat = CombatState.CreateCard<Retreat>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(approach);
            CardCmd.Upgrade(retreat);
        }

        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
            approach,
            PileType.Hand,
            Owner));
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
            retreat,
            PileType.Hand,
            Owner));
        await PlayerCmd.GainEnergy(DynamicVars[EnergyKey].IntValue, Owner);
    }

    protected override void OnUpgrade() { }
}

// Card-table ID 54: 猎获
public sealed class Hunt : CardModel
{
    private const string CardsKey = "Cards";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<HuntPower>(CardsKey, 2m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<NightMustStayMarkPower>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/hunt.png");

    public Hunt()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HuntPower>(
            context,
            Owner.Creature,
            DynamicVars[CardsKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 55: 凌波微步
public sealed class WaveWalking : CardModel
{
    private const string EnergyKey = "Energy";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new EnergyVar(EnergyKey, 1) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            EnergyHoverTip,
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/wave_walking.png");

    public WaveWalking()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WaveWalkingPower>(
            context,
            Owner.Creature,
            DynamicVars[EnergyKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 56: 弦上箭
public sealed class ArrowOnString : CardModel
{
    private const string DistanceKey = "Distance";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar(DistanceKey, 5m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<ArrowOnStringPower>(),
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/arrow_on_string.png");

    public ArrowOnString()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ArrowOnStringPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 57: 枯荣相生
public sealed class WitherAndFlourish : CardModel
{
    private const string ThresholdKey = "Threshold";
    private const string EnergyKey = "Energy";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar(ThresholdKey, 7m),
            new EnergyVar(EnergyKey, 1),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
            EnergyHoverTip,
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/wither_and_flourish.png");

    public WitherAndFlourish()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool hasEnoughHiddenPoison =
            (cardPlay.Target.GetPower<HiddenPoisonPower>()?.Amount ?? 0m)
            >= DynamicVars[ThresholdKey].BaseValue;
        await IroneyeHiddenPoison.BurstOnce(
            context,
            cardPlay.Target,
            Owner.Creature,
            this);
        if (hasEnoughHiddenPoison)
            await PlayerCmd.GainEnergy(DynamicVars[EnergyKey].IntValue, Owner);
    }

    protected override void OnUpgrade() =>
        DynamicVars[EnergyKey].UpgradeValueBy(1m);
}
