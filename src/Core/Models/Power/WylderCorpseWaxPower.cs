using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power;

public sealed class WylderCorpseWaxPower : PowerModel
{
    private sealed class Pulse { public int Damage; public int Turns; }
    private sealed class Data { public readonly List<Pulse> Pulses = new(); }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public static async Task ApplyScheduled(PlayerChoiceContext context, Creature target, CardModel source, int damage, int turns)
    {
        if (!target.IsAlive || damage <= 0 || turns <= 0) return;
        await PowerCmd.Apply<WylderCorpseWaxPower>(context, target, damage, source.Owner.Creature, source);
        // Debuff immunity may prevent application. Only register a successfully
        // applied portion, without extending previously applied durations.
        if (target.GetPower<WylderCorpseWaxPower>() is { } power)
        {
            int registered = power.GetInternalData<Data>().Pulses.Sum(pulse => pulse.Damage);
            int added = power.Amount - registered;
            if (added > 0) power.GetInternalData<Data>().Pulses.Add(new Pulse { Damage = added, Turns = turns });
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy || !participants.Contains(Owner) || !Owner.IsAlive) return;
        var pulses = GetInternalData<Data>().Pulses;
        int damage = pulses.Sum(pulse => pulse.Damage);
        if (damage <= 0) { await PowerCmd.Remove(this); return; }
        // Explicitly unpowered/unblockable: this is HP loss, not an attack.
        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, Owner, damage,
            ValueProp.Unpowered | ValueProp.Unblockable, null, null);
        if (!Owner.IsAlive) return;
        foreach (var pulse in pulses) pulse.Turns--;
        pulses.RemoveAll(pulse => pulse.Turns <= 0);
        int remaining = pulses.Sum(pulse => pulse.Damage);
        if (remaining == 0) await PowerCmd.Remove(this);
        else if (remaining != Amount) await PowerCmd.ModifyAmount(context, this, remaining - Amount, null, null);
    }
}
