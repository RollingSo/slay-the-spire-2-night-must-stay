using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderGrappleShot : WylderCard
{
    public WylderGrappleShot() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new PowerVar<VulnerablePower>("Vulnerable", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VulnerablePower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.PlayImmediate(Owner.Creature, "ClawShot");
        if (play.Target == Owner.Creature)
            await WylderPursuitPower.Enter(context, Owner.Creature, this);
        else if (play.Target?.IsAlive == true)
        {
            WylderAttackEffects.Play(play.Target, WylderAttackVfx.Kind.Claw, Owner.Creature);
            await PowerCmd.Apply<VulnerablePower>(context, play.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderIntenseBurning : WylderCard
{
    public WylderIntenseBurning() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VigorPower>("Vigor", 4m), new RepeatVar(0) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VigorPower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int count = ResolveEnergyXValue() + DynamicVars.Repeat.IntValue;
        for (int i = 0; i < count; i++)
            await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}
