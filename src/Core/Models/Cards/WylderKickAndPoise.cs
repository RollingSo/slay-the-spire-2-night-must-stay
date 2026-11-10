using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderKick:WylderCard
{
 public WylderKick():base(1,CardType.Skill,CardRarity.Uncommon,TargetType.AnyEnemy){}
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new PowerVar<PhantomImbalancePower>("Imbalance",1m),new EnergyVar(1),new CardsVar(1)};
 protected override IEnumerable<IHoverTip> ExtraHoverTips=>base.ExtraHoverTips.Append(HoverTipFactory.FromPower<PhantomImbalancePower>()).Append(HoverTipFactory.Static(StaticHoverTip.Stun));
 protected override bool ShouldGlowGoldInternal=>base.ShouldGlowGoldInternal||CanStunAnyEnemy;
 private bool CanStunAnyEnemy
 {
  get
  {
   if(!IsMutable||Owner==null||CombatState==null||Pile?.Type!=PileType.Hand)return false;
   int threshold=PhantomImbalancePower.GetThreshold(CombatState.Players.Count);
   decimal applied=DynamicVars["Imbalance"].BaseValue;
   return CombatState.GetOpponentsOf(Owner.Creature).Any(enemy=>enemy.IsAlive&&enemy.IsHittable&&!enemy.IsStunned&&enemy.GetPowerAmount<PhantomImbalancePower>()+applied>=threshold);
  }
 }
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
 {
  var target=p.Target;if(target?.IsAlive!=true)return;
  var previous=target.Monster?.NextMove;
  await PowerCmd.Apply<PhantomImbalancePower>(c,target,DynamicVars["Imbalance"].BaseValue,Owner.Creature,this);
  await PhantomImbalancePower.ResolveThreshold(c,target);
  if(target.IsAlive&&target.IsStunned&&!ReferenceEquals(previous,target.Monster?.NextMove)){
   await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue,Owner);
   await CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner);
  }
 }
 protected override void OnUpgrade()=>DynamicVars["Imbalance"].UpgradeValueBy(1m);
}
public sealed class WylderPoise:WylderCard
{
 public WylderPoise():base(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self){}
 public override bool GainsBlock=>true;
 public override IEnumerable<CardKeyword> CanonicalKeywords=>new[]{CardKeyword.Retain};
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new BlockVar(16m,ValueProp.Move),new DynamicVar("HandThreshold",2m)};
 protected override bool ShouldGlowGoldInternal=>base.ShouldGlowGoldInternal||WillMeetHandCondition;
 private bool WillMeetHandCondition=>IsMutable&&Owner!=null&&Pile?.Type==PileType.Hand&&PileType.Hand.GetPile(Owner).Cards.Count-1<=DynamicVars["HandThreshold"].IntValue;
 private bool MeetsHandConditionForPlay=>SourceHandCount is int count&&count-1<=DynamicVars["HandThreshold"].IntValue;
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){if(MeetsHandConditionForPlay)await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block.BaseValue,ValueProp.Move,p);}
 protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(4m);
}
