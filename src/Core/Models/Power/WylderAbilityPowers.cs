using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using NightMustStay.Core.Models.Cards;

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
public sealed class WylderNightFormPower : WylderAbilityPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}
public sealed class WylderEruptionPower : WylderAbilityPower
{
    internal async Task AfterShuffle(PlayerChoiceContext context)
    {
        Flash();
        await WylderPursuitPower.Enter(context, Owner, null, WylderEffectSource.PowerEffect);
    }
}
public sealed class WylderRemainingEmberPower : WylderAbilityPower { }
public sealed class WylderUndyingPower : WylderAbilityPower { }
public sealed class WylderSeamlessPower : WylderAbilityPower
{
    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("Block", Sum(WylderSeamless.BaseBlock, WylderSeamless.UpgradedBlock));
            return description;
        }
    }
}
public sealed class WylderMoonlightSwordPower : WylderAbilityPower { }


public sealed class WylderWildfirePower : WylderAbilityPower
{
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel cardSource)
    {
        if (amount <= 0 || power is not VigorPower || power.Owner != Owner) return;
        var enemies = Owner.CombatState.GetOpponentsOf(Owner).Where(enemy => enemy.IsAlive && enemy.IsHittable).ToArray();
        if (enemies.Length == 0) return;
        var target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        Flash();
        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, target, amount * Copies, ValueProp.Unpowered, Owner, null);
    }
}
