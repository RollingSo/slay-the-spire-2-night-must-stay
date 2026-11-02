using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NightMustStay.Core.Models.Power;

// Amount is the number of copies. Upgrade mix is saved independently so mixed
// basic/upgraded copies merge without rounding or losing their identities.
public abstract class WylderAbilityPower : PowerModel
{
    [SavedProperty] public int UpgradedCopies { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public int Copies => Amount;
    public int BasicCopies => System.Math.Max(0, Amount - UpgradedCopies);
    public int Sum(int normal, int upgraded) => normal * BasicCopies + upgraded * UpgradedCopies;
    public int Highest(int normal, int upgraded) => UpgradedCopies > 0 ? upgraded : Amount > 0 ? normal : 0;
}

public sealed class WylderEmberReservePower : WylderAbilityPower { }
public sealed class WylderClashEmbersPower : WylderAbilityPower { }
public sealed class WylderOpeningHuntPower : WylderAbilityPower { }
public sealed class WylderWindwardStancePower : WylderAbilityPower { }
public sealed class WylderBattleInstinctPower : WylderAbilityPower { }
public sealed class WylderBloodStillHotPower : WylderAbilityPower { }
public sealed class WylderFlameGuardStancePower : WylderAbilityPower { }
public sealed class WylderPursuitCommandPower : WylderAbilityPower { }
public sealed class WylderBattlefieldMightPower : WylderAbilityPower { }
public sealed class WylderGuardResurgencePower : WylderAbilityPower { }
public sealed class WylderSwiftBattleWillPower : WylderAbilityPower { }
public sealed class WylderRuinousGhostflamePower : WylderAbilityPower { }
public sealed class WylderPursuitStancePower : WylderAbilityPower { }
public sealed class WylderFieryBloodPower : WylderAbilityPower { }
public sealed class WylderVeteranEdgePower : WylderAbilityPower { }
public sealed class WylderUnyieldingSpiritPower : WylderAbilityPower { }
public sealed class WylderNightEdgePower : WylderAbilityPower { }
public sealed class WylderBoldnessPower : WylderAbilityPower { }
public sealed class WylderNightFormPower : WylderAbilityPower { }
public sealed class WylderRemainingEmberPower : WylderAbilityPower { }
public sealed class WylderUndyingPower : WylderAbilityPower { }

