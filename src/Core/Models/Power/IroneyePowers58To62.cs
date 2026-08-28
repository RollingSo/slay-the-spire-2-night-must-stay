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

namespace NightMustStay.Core.Models.Power;

public sealed class NowhereToHidePower : PowerModel
{
    private sealed class Data
    {
        public bool TriggeredAttackThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side == Owner.Side)
            GetInternalData<Data>().TriggeredAttackThisTurn = false;

        if (side != Owner.Side || Amount <= 0m)
            return;

        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        if (enemies.Length == 0)
            return;

        Flash();
        await PowerCmd.Apply<MarkPower>(
            new BlockingPlayerChoiceContext(),
            enemies,
            Amount,
            Owner,
            null);
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (Amount <= 0m
            || cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Card.Type != CardType.Attack
            || GetInternalData<Data>().TriggeredAttackThisTurn)
        {
            return;
        }

        GetInternalData<Data>().TriggeredAttackThisTurn = true;
        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        foreach (Creature enemy in enemies)
        {
            if (enemy.GetPower<MarkPower>() is { } mark)
                await mark.TriggerAll(context, Owner, cardPlay.Card);
        }

        Flash();
    }
}

public sealed class VolatilePoisonPower : PowerModel, IPoisonBurstTriggerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<MarkPower>(),
        };

    public async Task AfterPoisonBurstTriggered(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel cardSource)
    {
        if (!target.IsAlive || target.GetPower<MarkPower>() is not { } mark)
            return;

        Flash();
        int triggers = (int)Amount;
        for (int i = 0; i < triggers && target.IsAlive; i++)
            await mark.TriggerOne(choiceContext, Owner, cardSource);
    }
}
