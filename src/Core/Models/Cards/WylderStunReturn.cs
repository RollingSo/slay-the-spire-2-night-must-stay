using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace NightMustStay.Core.Models.Cards;

// Implemented by battle cards that return on a successful enemy stun. The
// observer carries no damage/cost values, so it is independent of card balance.
internal interface IWylderStunReturn
{
    Task OnEnemyStunned();
}

internal static class WylderStunReturn
{
    internal static async Task Notify(Creature target)
    {
        if (target?.IsAlive != true || !target.IsStunned || target.CombatState == null) return;
        foreach (var player in target.CombatState.Players.ToArray())
        {
            if (player.Creature.Side == target.Side || player.Creature.IsDead || player.PlayerCombatState == null) continue;
            WylderStunHistory.Record(player);
            if (player.Creature.GetPower<WylderEndlessWillPower>() is { } will) await will.Trigger();
            if (player.Creature.GetPower<WylderFatesJudgmentPower>() is { } judgment) await judgment.Trigger(target);
            foreach (var card in player.PlayerCombatState.AllCards.OfType<IWylderStunReturn>().ToArray())
                await card.OnEnemyStunned();
        }
    }
}
