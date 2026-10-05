using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Power;

// Scheduled actions use native powers for hover tips and lifecycle, like family actions.
public abstract class RevenantNecroActionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            DynamicVars.AddTo(description);
            description.Add("Damage", Revenant.RevenantFamilyAttackIntent.CalculatePoweredDamage(
                Owner, DynamicVars["Damage"].IntValue));
            return description;
        }
    }
    protected virtual int BaseDamage => 8;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new("Damage", BaseDamage), new("Hits", 1) };
}

public sealed class NecroAttackPower : RevenantNecroActionPower { }
public sealed class NecroProtectPower : RevenantNecroActionPower
{
    protected override int BaseDamage => 3;
}
