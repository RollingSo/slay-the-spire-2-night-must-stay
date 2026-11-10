using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
internal static partial class WylderBattleArtRegression
{
 private static readonly List<CardModel> FinalAuto=new();
 private static void FinalThreeCards()
 {
  int start=_checks;
  var h=new HarmonyLib.Harmony("wylder.final.three");
  h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderGrantedDesperationPatch")!).Patch();
  h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderGrantedDesperationTextPatch")!).Patch();
  h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderGrantedDesperationHoverPatch")!).Patch();
  h.Patch(AccessTools.Method(typeof(CardSelectCmd),nameof(CardSelectCmd.FromHand)),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(NovemberSelect)));
  h.Patch(AccessTools.Method(typeof(CardCmd),nameof(CardCmd.AutoPlay)),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(FinalAutoPlay)));
  try
  {
   Fixture();var loneLastResort=Card(ModelDb.Card<WylderLastResort>(),false);Put(loneLastResort,PileType.Hand);
   Check(!(bool)AccessTools.PropertyGetter(typeof(CardModel),"IsPlayable").Invoke(loneLastResort,null)!,"Last Resort cannot open an empty selector as the sole hand card");
   var lastResortTarget=Card(ModelDb.Card<WylderStrike>(),false);Put(lastResortTarget,PileType.Hand);
   Check((bool)AccessTools.PropertyGetter(typeof(CardModel),"IsPlayable").Invoke(loneLastResort,null)!,"Last Resort is playable with another hand card");
   foreach(bool up in new[]{false,true})
   {
    foreach(var model in new CardModel[]{ModelDb.Card<WylderEndlessWill>(),ModelDb.Card<WylderLastResort>(),ModelDb.Card<WylderShriekOfMilos>()})
    {
     Fixture();var card=Card(model,up);
     Check(card.EnergyCost.GetResolved()==(up?0:1)&&card.TargetType==TargetType.Self,"Final three cost upgrade and targeting");
     Check(card.Type==(card is WylderEndlessWill?CardType.Power:CardType.Skill)&&card.Rarity==(card is WylderEndlessWill?CardRarity.Uncommon:CardRarity.Rare),"Final three type and rarity");
     Check(card.Keywords.Contains(CardKeyword.Exhaust)==(card is WylderLastResort),"Only Last Resort has native Exhaust");
    }
    Fixture();Play(Card(ModelDb.Card<WylderShriekOfMilos>(),up));
    Check(_enemies.All(e=>e.GetPowerAmount<VulnerablePower>()==1),"Milos applies 1 Vulnerable to every enemy");
    var shriek=_owner.Creature.GetPower<WylderShriekOfMilosPower>()!;
    Play(Card(ModelDb.Card<WylderDefend>(),false));Check(shriek.Amount==1,"Skill does not consume next Attack buff");
    var attack=Card(ModelDb.Card<WylderDesperateFlurry>(),false);attack.DynamicVars.Damage.UpdateCardPreview(attack,CardPreviewMode.Normal,_enemies[0],true);
    Check(attack.DynamicVars.Damage.PreviewValue==27,"Milos native damage preview doubles alongside Vulnerable");
    Play(attack);Check(ResolvedDamage.SequenceEqual(new[]{27m,27m,27m}),"Milos covers all native hits of next Attack");
    shriek.AfterCardPlayedLate(null!,TrackingArrowRegression.MakePlay(attack,_owner)).GetAwaiter().GetResult();
    Check(!_owner.Creature.HasPower<WylderShriekOfMilosPower>(),"Milos consumed after full Attack");
    Attacks.Clear();ResolvedDamage.Clear();Play(Card(ModelDb.Card<WylderStrike>(),false));Check(ResolvedDamage.Single()==9,"Following Attack not doubled");
    Fixture();var will=Card(ModelDb.Card<WylderEndlessWill>(),up);Play(will);
    var power=_owner.Creature.GetPower<WylderEndlessWillPower>()!;
    ((Task)AccessTools.Method(typeof(WylderEndlessWillPower),"Trigger").Invoke(power,null)!).GetAwaiter().GetResult();
    Check(_owner.Creature.HasPower<WylderPursuitPower>(),"Endless Will enters Pursuit");
    Play(Card(ModelDb.Card<WylderStrike>(),false));Check(!_owner.Creature.HasPower<WylderPursuitPower>(),"Pursuit consumed normally");
    ((Task)AccessTools.Method(typeof(WylderEndlessWillPower),"Trigger").Invoke(power,null)!).GetAwaiter().GetResult();Check(_owner.Creature.HasPower<WylderPursuitPower>(),"Endless Will repeatedly reenters");
    Fixture();_payment=Card(ModelDb.Card<WylderStrike>(),false);Put(_payment,PileType.Hand);var other=Card(ModelDb.Card<WylderDefend>(),false);Put(other,PileType.Hand);
    Play(Card(ModelDb.Card<WylderLastResort>(),up));
    Check(_payment.EnergyCost.GetResolved()==2&&other.EnergyCost.GetResolved()==1,"Last Resort raises only selected copy cost");
    var helper=typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderGrantedDesperation")!;
    Check((bool)AccessTools.Method(helper,"Is").Invoke(null,new object[]{_payment})!&&!(bool)AccessTools.Method(helper,"Is").Invoke(null,new object[]{other})!,"Last Resort tags only selected copy");
    FinalAuto.Clear();_payment.BeforeSideTurnEnd(null!,CombatSide.Player,new[]{_owner.Creature}).GetAwaiter().GetResult();Check(FinalAuto.Count==0,"Normal hand card blocks granted Desperation");
    Put(other,PileType.Discard);_payment.BeforeSideTurnEnd(null!,CombatSide.Player,new[]{_owner.Creature}).GetAwaiter().GetResult();Check(FinalAuto.SequenceEqual(new[]{_payment}),"Granted Desperation actually auto-plays selected ordinary card");
    Check(_payment.EnergyCost.GetResolved()==2,"Raised cost persists after play");
    Fixture();_payment=Card(ModelDb.Card<WylderIntenseBurning>(),false);Put(_payment,PileType.Hand);Play(Card(ModelDb.Card<WylderLastResort>(),up));
    Check((bool)AccessTools.Method(helper,"Is").Invoke(null,new object[]{_payment})! && _payment.EnergyCost.CostsX && _payment.EnergyCost.HasLocalModifiers,"X card gains Desperation and the unconditional cost modifier");
    Fixture();_payment=Card(ModelDb.Card<WylderStrike>(),false);Put(_payment,PileType.Hand);Play(Card(ModelDb.Card<WylderLastResort>(),up));Play(Card(ModelDb.Card<WylderLastResort>(),up));
    Check(_payment.EnergyCost.GetResolved()==3,"Repeated Last Resort adds cost without duplicate marker");
    var native=Card(ModelDb.Card<WylderEndTheNight>(),false);Put(native,PileType.Hand);FinalAuto.Clear();
    _payment.BeforeSideTurnEnd(null!,CombatSide.Player,new[]{_owner.Creature}).GetAwaiter().GetResult();native.BeforeSideTurnEnd(null!,CombatSide.Player,new[]{_owner.Creature}).GetAwaiter().GetResult();
    Check(FinalAuto.SequenceEqual(new[]{_payment,native}),"Mixed granted and native Desperation each auto-play exactly once");
   }
  }finally{h.UnpatchAll(h.Id);}
  Console.WriteLine($"PASS: {_checks-start} final-three checks: cost upgrades, native damage preview/multihit, Vulnerable, buff consumption, repeated Pursuit and granted Desperation autoplay.");
 }
 public static bool FinalAutoPlay(CardModel card,ref Task __result){FinalAuto.Add(card);Put(card,PileType.Discard);__result=Task.CompletedTask;return false;}
}
