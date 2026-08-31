using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power;

public interface IMarkTriggerPower
{
    Task AfterMarkTriggered(
        PlayerChoiceContext choiceContext,
        Creature markedTarget,
        CardModel triggeringCard);
}

public sealed class FinalBattleNoBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.Static(StaticHoverTip.Block) };

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel cardSource,
        CardPlay cardPlay) =>
        target == Owner ? 0m : 1m;
}

public sealed class HuntPower : PowerModel, IMarkTriggerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public async Task AfterMarkTriggered(
        PlayerChoiceContext choiceContext,
        Creature markedTarget,
        CardModel triggeringCard)
    {
        if (Owner.Player == null || Amount <= 0)
            return;

        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}

public sealed class WaveWalkingPower : PowerModel
{
    private const decimal DistanceThreshold = 4m;

    private sealed class Data
    {
        public decimal DistanceMoved;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Match Regent's Orbit: show progress remaining to the next trigger while
    // Amount continues to represent the Energy gained per trigger.
    public override int DisplayAmount => decimal.ToInt32(
        DistanceThreshold - GetInternalData<Data>().DistanceMoved % DistanceThreshold);

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<DistancePower>() };

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (power is not DistancePower
            || power.Owner != Owner
            || amount == 0m
            || Owner.Player == null
            || Amount <= 0)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.DistanceMoved += decimal.Abs(amount);
        while (data.DistanceMoved >= DistanceThreshold)
        {
            data.DistanceMoved -= DistanceThreshold;
            Flash();
            await PlayerCmd.GainEnergy(Amount, Owner.Player);
        }
        InvokeDisplayAmountChanged();
    }
}

public sealed class ArrowOnStringPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<DistancePower>(),
        };

    public override bool ShouldClearBlock(Creature creature) =>
        creature != Owner;

    public override Task AfterPreventingBlockClear(
        AbstractModel preventer,
        Creature creature)
    {
        if (creature == Owner)
            Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side || !participants.Contains(Owner))
            return;

        await PowerCmd.Apply<DistancePower>(
            new BlockingPlayerChoiceContext(),
            Owner,
            -5m,
            Owner,
            null);
        await PowerCmd.Remove(this);
    }
}
