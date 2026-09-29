using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NightMustStay.Core.Models.Power;

public sealed class DuchessEternalFormPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public async Task OnMomentEffect()
    {
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}

public sealed class DuchessShadowSwordPower : PowerModel
{
    [SavedProperty]
    public decimal AppliedStrength { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterApplied(Creature applier, CardModel cardSource) =>
        Synchronize(new BlockingPlayerChoiceContext());

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel cardSource) =>
        power.Owner == Owner && (power is DuchessConcealmentPower || power == this)
            ? Synchronize(context) : Task.CompletedTask;

    public async Task Synchronize(PlayerChoiceContext context)
    {
        decimal desired = Owner.GetPower<DuchessConcealmentPower>() is { Amount: > 0 } ? Amount : 0;
        decimal delta = desired - AppliedStrength;
        if (delta == 0) return;
        AppliedStrength = desired;
        await PowerCmd.Apply<StrengthPower>(context, Owner, delta, Owner, null);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        decimal previous = AppliedStrength;
        AppliedStrength = 0;
        if (previous != 0)
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), oldOwner, -previous, oldOwner, null);
    }
}
