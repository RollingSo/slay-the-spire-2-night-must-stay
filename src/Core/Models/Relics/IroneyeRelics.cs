using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Relics;

public abstract class IroneyeRelicModel : RelicModel
{
    protected abstract string IroneyeIconName { get; }

    public override string PackedIconPath =>
        $"res://ironeye_assets/relics/{IroneyeIconName}.png";

    protected override string PackedIconOutlinePath => PackedIconPath;

    protected override string BigIconPath => PackedIconPath;

    public override bool ShouldFlashOnPlayer => false;
}

public sealed class CrackedSealingWax : IroneyeRelicModel
{
    protected override string IroneyeIconName => "cracked_sealing_wax";

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<MarkPower>(2m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)
            || Owner.PlayerCombatState.TurnNumber > 1)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<MarkPower>(
            choiceContext,
            combatState.HittableEnemies,
            DynamicVars[nameof(MarkPower)].BaseValue,
            Owner.Creature,
            null);
    }
}

public sealed class WisdomsDarkNight : IroneyeRelicModel
{
    protected override string IroneyeIconName => "wisdoms_dark_night";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<HiddenPoisonPower>(2m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<HiddenPoisonPower>() };

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)
            || Owner.PlayerCombatState.TurnNumber > 1)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<HiddenPoisonPower>(
            choiceContext,
            combatState.HittableEnemies,
            DynamicVars[nameof(HiddenPoisonPower)].BaseValue,
            Owner.Creature,
            null);
    }
}

public sealed class ProtectiveScaleArmor : IroneyeRelicModel
{
    protected override string IroneyeIconName => "protective_scale_armor";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new BlockVar(5m, ValueProp.Unpowered) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<MarkPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
        };

    public async Task AfterMarkTriggered(PlayerChoiceContext choiceContext)
    {
        Flash();
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block,
            null);
    }
}

public sealed class FarArrowTalisman : IroneyeRelicModel
{
    private const string DexterityKey = "Dexterity";
    private bool _active;

    protected override string IroneyeIconName => "far_arrow_talisman";

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<DexterityPower>(DexterityKey, 2m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<DexterityPower>(),
        };

    [SavedProperty]
    public bool IsActive
    {
        get => _active;
        set
        {
            AssertMutable();
            _active = value;
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (power is not DistancePower || power.Owner != Owner.Creature)
            return;

        bool shouldBeActive = power.Amount > 0m;
        if (shouldBeActive == IsActive)
            return;

        IsActive = shouldBeActive;
        Flash();
        decimal delta = DynamicVars[DexterityKey].BaseValue
            * (shouldBeActive ? 1m : -1m);
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner.Creature,
            delta,
            Owner.Creature,
            cardSource);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        IsActive = false;
        return Task.CompletedTask;
    }
}

public sealed class HardArrowTalisman : IroneyeRelicModel
{
    private bool _triggeredThisTurn;

    protected override string IroneyeIconName => "hard_arrow_talisman";

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new EnergyVar(1) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<LongShotPower>(),
            HoverTipFactory.ForEnergy(this),
        };

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
            _triggeredThisTurn = false;

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (_triggeredThisTurn
            || cardPlay.Card.Owner != Owner
            || cardPlay.Card is not ILongShotCard
            || (Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m) < 2m)
        {
            return;
        }

        _triggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}

public sealed class SacredRhythmBlade : IroneyeRelicModel
{
    private const string TriggerRangeKey = "TriggerRange";

    protected override string IroneyeIconName => "sacred_rhythm_blade";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public decimal TriggerRangeIncrease =>
        DynamicVars[TriggerRangeKey].BaseValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar(TriggerRangeKey, 1m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };
}

public sealed class GlidingGarb : IroneyeRelicModel
{
    private const int DistanceThreshold = 10;
    private int _distanceProgress;

    protected override string IroneyeIconName => "gliding_garb";

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShowCounter => true;

    public override int DisplayAmount => DistanceProgress;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new EnergyVar(1),
            new DynamicVar("Distance", DistanceThreshold),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.ForEnergy(this),
        };

    [SavedProperty]
    public int DistanceProgress
    {
        get => _distanceProgress;
        set
        {
            AssertMutable();
            _distanceProgress = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (power is not DistancePower
            || power.Owner != Owner.Creature
            || amount == 0m)
        {
            return;
        }

        DistanceProgress += decimal.ToInt32(decimal.Abs(amount));
        while (DistanceProgress >= DistanceThreshold)
        {
            DistanceProgress -= DistanceThreshold;
            Flash();
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        DistanceProgress = 0;
        return Task.CompletedTask;
    }
}
