using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NightMustStay.Core.Models.Power;

public sealed class StrangleCommandPower : PowerModel
{
    private sealed class Data { public readonly Dictionary<Creature, decimal> Applied = new(); }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    internal static decimal PositiveDistanceStrength(decimal distance, bool bladeShadow) =>
        distance < 0 ? -distance * (bladeShadow ? 2 : 1) : 0;

    public override Task AfterApplied(Creature applier, CardModel cardSource) =>
        Refresh(new BlockingPlayerChoiceContext(), cardSource);

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel cardSource) =>
        power.Owner == Owner && (power is DistancePower || power is BladeShadowUnmatchedPower || power == this)
            ? Refresh(context, cardSource) : Task.CompletedTask;

    private async Task Refresh(PlayerChoiceContext context, CardModel source)
    {
        if (Owner?.CombatState == null) return;
        decimal baseStrength = PositiveDistanceStrength(Owner.GetPower<DistancePower>()?.Amount ?? 0,
            Owner.HasPower<BladeShadowUnmatchedPower>());
        foreach (var player in Owner.CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
        {
            var target = player.Creature;
            // Owner already has the native contribution; add only its duplicate.
            decimal desired = baseStrength * (target == Owner ? 1 : 2);
            var applied = GetInternalData<Data>().Applied;
            decimal delta = desired - applied.GetValueOrDefault(target);
            if (delta == 0) continue;
            applied[target] = desired; // Record before dispatching reentrant hooks.
            await PowerCmd.Apply<StrengthPower>(context, target, delta, Owner, source);
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        var applied = GetInternalData<Data>().Applied;
        var changes = applied.ToArray();
        applied.Clear();
        foreach (var change in changes)
            if (change.Key.IsAlive && change.Key.CombatState != null && change.Value != 0)
                await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), change.Key,
                    -change.Value, oldOwner, null);
    }
}
