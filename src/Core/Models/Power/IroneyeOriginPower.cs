using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power;

public sealed class IroneyeOriginPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    internal static bool IsReturningToOrigin(decimal before, decimal after) => before != 0m && after == 0m;
    // Called once by DistancePower after its actual nonzero -> zero transition,
    // after Dexterity/Strength synchronization; no trigger on simply crossing zero.
    public async Task ReturnToOrigin(PlayerChoiceContext context, CardModel source)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
