using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Power;

/// <summary>
/// Marker used by the summon manager to replace the family's currently
/// previewed action without touching Osty's built-in powers.
/// </summary>
public interface IRevenantFamilyActionPower
{
}

public abstract class RevenantFamilyActionPower : PowerModel, IRevenantFamilyActionPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

public sealed class HelenStepStrikePower : RevenantFamilyActionPower { }
public sealed class HelenRetreatPower : RevenantFamilyActionPower { }
public sealed class FrederickHeavyHammerPower : RevenantFamilyActionPower { }
public sealed class FrederickHeadbuttPower : RevenantFamilyActionPower { }
public sealed class SebastianRoarPower : RevenantFamilyActionPower { }
public sealed class SebastianSlamPower : RevenantFamilyActionPower { }
