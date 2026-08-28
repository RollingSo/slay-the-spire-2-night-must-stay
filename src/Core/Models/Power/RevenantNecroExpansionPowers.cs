using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
namespace NightMustStay.Core.Models.Power;

public sealed class GhostlyTouchPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterSummonActed(PlayerChoiceContext context)
    {
        Creature[] enemies = Owner.CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        if (enemies.Length == 0)
            return;
        Creature target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        Flash();
        await PowerCmd.Apply<FreezePower>(context, target, Amount, Owner, null);
    }
}
