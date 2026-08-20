using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Patches;

namespace sts2mod.Core.Models.Cards;

// Card-table ID 42: 迫近毒牙
public sealed class ApproachingVenomFang : CardModel
{
    private const string HiddenPoisonKey = "HiddenPoison";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<ApproachingVenomFangPower>(HiddenPoisonKey, 1m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/approaching_venom_fang.png");

    public ApproachingVenomFang()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ApproachingVenomFangPower>(
            context,
            Owner.Creature,
            DynamicVars[HiddenPoisonKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 43: 万物凋零
public sealed class AllThingsWither : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/all_things_wither.png");

    public AllThingsWither()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AllThingsWitherPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 44: 进与退
public sealed class AdvanceAndRetreat : CardModel
{
    private const string DistanceKey = "Distance";
    private const string MarkKey = "Mark";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar(DistanceKey, 1m),
            new BlockVar(5m, ValueProp.Move),
            new PowerVar<MarkPower>(MarkKey, 1m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<MarkPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/advance_and_retreat.png");

    public AdvanceAndRetreat()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (cardPlay.Target == Owner.Creature)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
            return;
        }

        await PowerCmd.Apply<DistancePower>(
            context,
            Owner.Creature,
            -DynamicVars[DistanceKey].BaseValue,
            Owner.Creature,
            this);
        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<MarkPower>(
                context,
                cardPlay.Target,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars[MarkKey].UpgradeValueBy(1m);
    }
}

// Card-table ID 45: 警觉
public sealed class Vigilance : CardModel
{
    private const string CardsKey = "Cards";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar(CardsKey, 3m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromCard<Retreat>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/vigilance.png");

    public Vigilance()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(context, DynamicVars[CardsKey].IntValue, Owner);
        CardModel selected = (await CardSelectCmd.FromHand(
                context,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                null,
                this))
            .FirstOrDefault();
        if (selected == null)
            return;

        CardModel retreat = CombatState.CreateCard<Retreat>(Owner);
        await CardCmd.Transform(selected, retreat);
    }

    protected override void OnUpgrade() =>
        DynamicVars[CardsKey].UpgradeValueBy(1m);
}

// Card-table ID 46: 来时路
public sealed class RoadAlreadyTraveled : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new CalculationBaseVar(4m),
            new CalculationExtraVar(2m),
            new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
                static (card, _) =>
                    card.Owner.Creature.GetPower<DistancePower>()
                        ?.DistanceMovedThisTurn ?? 0m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/road_already_traveled.png");

    public RoadAlreadyTraveled()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        decimal block = DynamicVars.CalculatedBlock.Calculate(null);
        if (block > 0m)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                block,
                DynamicVars.CalculatedBlock.Props,
                cardPlay);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars["CalculationBase"].UpgradeValueBy(3m);
}

// Card-table ID 47: 鬼步
public sealed class HeavenlyEyeForm : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Approach>(true)
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>(true))
            .Concat(IsUpgraded ? new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) } : Array.Empty<IHoverTip>());

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/heavenly_eye_form.png");

    public HeavenlyEyeForm()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HeavenlyEyeFormPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Card-table ID 48: 共享情报
public sealed class SharedIntelligence : CardModel
{
    private const string TriggersKey = "Triggers";

    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<SharedIntelligencePower>(TriggersKey, 1m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/shared_intelligence.png");

    public SharedIntelligence()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SharedIntelligencePower>(
            context,
            Owner.Creature,
            DynamicVars[TriggersKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars[TriggersKey].UpgradeValueBy(1m);
}

// Card-table ID 49: 铁之眼
public sealed class IronEye : CardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/iron_eye.png");

    public IronEye()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IronEyePower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        EnergyCost.UpgradeBy(-1);
}

// Card-table ID 50: 观察力
public sealed class Observation : CardModel
{
    private int _pendingRewardUpgrades;

    [SavedProperty]
    public int PendingRewardUpgrades
    {
        get => _pendingRewardUpgrades;
        set
        {
            AssertMutable();
            _pendingRewardUpgrades = value;
        }
    }

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/observation.png");

    public Observation()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (DeckVersion is Observation deckVersion)
            deckVersion.PendingRewardUpgrades++;

        await PowerCmd.Apply<ObservationPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    public override bool TryModifyCardRewardOptionsLate(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions options)
    {
        if (player != Owner
            || PendingRewardUpgrades <= 0
            || options.Flags.HasFlag(CardCreationFlags.NoHookUpgrades))
        {
            return false;
        }

        List<CardCreationResult> valid = cardRewards
            .Where(result => result.Card.IsUpgradable
                && !ObservationRewardUpgradeRegistry.IsPending(result.Card))
            .ToList();
        if (valid.Count == 0)
            return false;

        int upgrades = Math.Min(PendingRewardUpgrades, valid.Count);
        for (int i = 0; i < upgrades; i++)
        {
            CardCreationResult selected = Owner.RunState.Rng.Niche.NextItem(valid);
            valid.Remove(selected);
            CardModel normalClone = Owner.RunState.CloneCard(selected.Card);
            CardModel upgradedClone = Owner.RunState.CloneCard(normalClone);
            CardCmd.Upgrade(upgradedClone, CardPreviewStyle.None);
            selected.ModifyCard(upgradedClone);
            ObservationRewardUpgradeRegistry.Mark(upgradedClone, normalClone);
            PendingRewardUpgrades--;
        }

        return upgrades > 0;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
