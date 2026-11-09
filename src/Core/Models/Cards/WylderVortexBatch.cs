using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderHorizontalSlash:WylderCard
{
 public WylderHorizontalSlash():base(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy){}
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new DamageVar(10m,ValueProp.Move),new DynamicVar("Break",20m)};
 protected override decimal BreakThreshold=>DynamicVars["Break"].BaseValue;
 protected override async Task OnBreak(PlayerChoiceContext c)=>await WylderPursuitPower.Enter(c,Owner.Creature,this);
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){WylderAnimationPatch.QueueAttackMotion(Owner.Creature,"AttackSingle");await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).WithWylderFx(WylderAttackVfx.Kind.Greatsword,Owner.Creature).Execute(c);}
 protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3m);
}
public sealed class WylderVerticalCleave:WylderFollowThroughCard
{
 public WylderVerticalCleave():base(2,CardRarity.Common){}
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new DamageVar(12m,ValueProp.Move)};
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){WylderAnimationPatch.QueueAttackMotion(Owner.Creature,"AttackTwoHand");await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).WithWylderFx(WylderAttackVfx.Kind.Greatsword,Owner.Creature).Execute(c);}
 protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(4m);
}
public sealed class WylderOrdovisVortex:WylderCard
{
 private AttackCommand _resolvingAttack;
 public WylderOrdovisVortex():base(1,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy){}
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new DamageVar(11m,ValueProp.Move),new DynamicVar("Break",20m),new RepeatVar(2)};
 protected override decimal BreakThreshold=>DynamicVars["Break"].BaseValue;
 internal bool AddsHits(AttackCommand command)=>ReferenceEquals(command,_resolvingAttack)&&BrokeThisPlay;
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
 {
  WylderAnimationPatch.QueueAttackMotion(Owner.Creature,"AttackTwoHand");
  _resolvingAttack=DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).WithHitCount(1).WithWylderFx(WylderAttackVfx.Kind.OrdovisVortex,Owner.Creature);
  try{await _resolvingAttack.Execute(c);}finally{_resolvingAttack=null;}
 }
 protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3m);
}
internal static class WylderStunHistory
{
 private sealed class State{internal readonly Dictionary<Player,int> Counts=new();}
 private static readonly ConditionalWeakTable<object,State> States=new();
 internal static int Count(Player p)=>p?.Creature?.CombatState==null?0:States.GetOrCreateValue(p.Creature.CombatState).Counts.GetValueOrDefault(p);
 internal static void Record(Player p){var s=States.GetOrCreateValue(p.Creature.CombatState);s.Counts[p]=s.Counts.GetValueOrDefault(p)+1;}
}
public sealed class WylderSiluriaVortex:WylderCard
{
 public WylderSiluriaVortex():base(2,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy){}
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new CalculationBaseVar(24m),new ExtraDamageVar(5m),new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static(card,_)=>WylderStunHistory.Count(card.Owner))};
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){WylderAnimationPatch.QueueAttackMotion(Owner.Creature,"AttackTwoHand");await DamageCmd.Attack(DynamicVars.CalculatedDamage.Calculate(p.Target)).CompatFromCard(this).Targeting(p.Target).WithWylderFx(WylderAttackVfx.Kind.SiluriaVortex,Owner.Creature).Execute(c);}
 protected override void OnUpgrade()=>DynamicVars["ExtraDamage"].UpgradeValueBy(5m);
}
