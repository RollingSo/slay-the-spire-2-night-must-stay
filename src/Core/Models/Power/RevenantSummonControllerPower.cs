using System.Threading.Tasks;
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

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player)
            await RevenantSummonManager.For(player).TriggerFamilyNormalAction(context);
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext context,
        Creature dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel cardSource)
    {
        if (dealer == Owner && result.WasTargetKilled)
            RevenantSummonManager.For(Owner.Player).TryRegisterNecro(target);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RevenantSummonManager.For(Owner.Player).CleanupVisuals();
        RevenantSummonManager.Clear(Owner.Player);
        return Task.CompletedTask;
    }
}
