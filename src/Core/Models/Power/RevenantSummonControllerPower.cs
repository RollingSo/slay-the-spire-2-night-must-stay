using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Models.Power;

public sealed class RevenantSummonControllerPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // The controller is an implementation detail; its hooks remain active, but
    // it should never occupy a visible power slot in the combat UI.
    protected override bool IsVisibleInternal => false;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player)
            return;

        RevenantSummonManager manager = RevenantSummonManager.For(player);
        await manager.SummonMarkedNecro(context);
        await manager.ExecuteScheduledFamilyAction(context);
        await manager.ScheduleFamilyNormalAction(context);
        await manager.TriggerAllNecros(context);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext context,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (result.WasTargetKilled)
            await RevenantSummonManager.For(Owner.Player).HandleFamilyDeath(target);
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext context,
        Creature dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel cardSource)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(Owner.Player);
        if ((dealer == Owner || manager.IsFamilyCreature(dealer) || manager.GetLivingNecros().Any(necro => necro.Creature == dealer)) && result.WasTargetKilled)
            manager.TryRegisterNecro(target);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RevenantSummonManager.For(Owner.Player).CleanupVisuals();
        RevenantSummonManager.Clear(Owner.Player);
        return Task.CompletedTask;
    }
}
