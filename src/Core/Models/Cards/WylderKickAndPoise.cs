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
 internal bool Eligible=>Owner==null||CombatState==null||LowHand(1);
 protected override IEnumerable<DynamicVar> CanonicalVars=>new DynamicVar[]{new CalculationBaseVar(0m),new CalculationExtraVar(16m),new PoiseBlockVar().WithMultiplier(static(card,_)=>((WylderPoise)card).Eligible?1m:0m)};
 protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){if(Eligible)await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.CalculatedBlock.Calculate(null),ValueProp.Move,p);}
 protected override void OnUpgrade()=>DynamicVars.CalculationExtra.UpgradeValueBy(4m);
 private sealed class PoiseBlockVar:CalculatedBlockVar
 {
  public PoiseBlockVar():base(ValueProp.Move){}
  public override void UpdateCardPreview(MegaCrit.Sts2.Core.Models.CardModel card,CardPreviewMode mode,Creature target,bool hooks){base.UpdateCardPreview(card,mode,target,hooks);if(!((WylderPoise)card).Eligible)PreviewValue=0m;}
 }
}
