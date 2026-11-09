using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
internal static class WylderMultiplayerBatchRegression
{
 static CardModel? Selected, Transformed;
 static readonly List<CardModel> Generated=new();
 static readonly List<(PowerModel power,Creature target,decimal amount)> Applied=new();
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 public static void Run()
 {
  foreach(var (model,cost,type,rarity) in new (CardModel,int,CardType,CardRarity)[]{
   (ModelDb.Card<WylderMultiplayerCard>(),0,CardType.Power,CardRarity.Rare),
   (ModelDb.Card<WylderTinder>(),0,CardType.Skill,CardRarity.Common),
   (ModelDb.Card<WylderDefiance>(),2,CardType.Skill,CardRarity.Common),
   (ModelDb.Card<WylderBreakthrough>(),0,CardType.Skill,CardRarity.Common),
   (ModelDb.Card<WylderHoarfrostStomp>(),1,CardType.Skill,CardRarity.Uncommon),
   (ModelDb.Card<WylderOffensiveInstinct>(),1,CardType.Power,CardRarity.Uncommon)})
  {
   var c=model.ToMutable();Check(c.EnergyCost.GetResolved()==cost && c.Type==type && c.Rarity==rarity,"Metadata "+c.Id);
   Check(c.TargetType==(c is WylderBreakthrough?TargetType.AnyEnemy:TargetType.Self),"Target "+c.Id);
   if(c is WylderDefiance)Check(c.DynamicVars.Block.BaseValue==19,"Defiance base");
   if(c is WylderBreakthrough)Check(c.Keywords.Contains(CardKeyword.Exhaust)&&c.DynamicVars["Vulnerable"].BaseValue==1&&c.DynamicVars["Vigor"].BaseValue==4,"Breakthrough base");
   if(c is WylderHoarfrostStomp)Check(c.DynamicVars["Freeze"].BaseValue==2,"Stomp base");
   c.UpgradeInternal();
   if(c is WylderDefiance)Check(c.DynamicVars.Block.BaseValue==23,"Defiance upgrade");
   if(c is WylderBreakthrough)Check(c.DynamicVars["Vulnerable"].BaseValue==2&&c.DynamicVars["Vigor"].BaseValue==5,"Breakthrough upgrade");
   if(c is WylderHoarfrostStomp)Check(c.DynamicVars["Freeze"].BaseValue==3,"Stomp upgrade");
   if(c is WylderOffensiveInstinct)Check(c.EnergyCost.GetResolved()==0,"Instinct upgrade");
  }
  Check(ModelDb.Card<WylderRuinousGhostflame>().Rarity==CardRarity.Uncommon,"Ghostflame uncommon");
  Check(ModelDb.Card<WylderMultiplayerCard>().MultiplayerConstraint==CardMultiplayerConstraint.MultiplayerOnly,"Multiplayer-only rewards");
  var h=new HarmonyLib.Harmony("wylder.batch.new");
  h.Patch(AccessTools.Method(typeof(CombatState),"CreateCard",new[]{typeof(CardModel),typeof(Player)}),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Create)));
  h.Patch(AccessTools.Method(typeof(CardPileCmd),nameof(CardPileCmd.AddGeneratedCardToCombat)),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Add)));
  h.Patch(AccessTools.Method(typeof(CardCmd),nameof(CardCmd.Upgrade),new[]{typeof(CardModel),typeof(CardPreviewStyle)}),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Upgrade)));
  h.Patch(typeof(PowerCmd).GetMethods().Single(m=>m.Name=="Apply"&&!m.IsGenericMethod),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Apply)));
  h.Patch(AccessTools.Method(typeof(CardSelectCmd),nameof(CardSelectCmd.FromHand)),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Select)));
  h.Patch(AccessTools.Method(typeof(CardCmd),nameof(CardCmd.Transform),new[]{typeof(CardModel),typeof(CardModel),typeof(CardPreviewStyle)}),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Transform)));
  h.Patch(AccessTools.PropertyGetter(typeof(CardModel),nameof(CardModel.CombatState)),prefix:new HarmonyMethod(typeof(WylderMultiplayerBatchRegression),nameof(Combat)));
  try {
   var combat=new CombatState();
   Player Make(){var p=TrackingArrowRegression.MakePlayer();AccessTools.Field(typeof(Player),"<Character>k__BackingField").SetValue(p,ModelDb.Character<Wylder>());AccessTools.Field(typeof(Player),"_runState").SetValue(p,NullRunState.Instance);var cr=new Creature(p,50,50){CombatState=combat};AccessTools.Field(typeof(Player),"<Creature>k__BackingField").SetValue(p,cr);((List<Creature>)AccessTools.Field(typeof(CombatState),"_allies").GetValue(combat)!).Add(cr);return p;}
   var owner=Make();var ally1=Make();var ally2=Make();
   Selected=ModelDb.Card<WylderStrike>().ToMutable();AccessTools.Field(typeof(CardModel),"_owner").SetValue(Selected,owner);
   foreach(bool upgraded in new[]{false,true}) {
    var tinder=ModelDb.Card<WylderTinder>().ToMutable();AccessTools.Field(typeof(CardModel),"_owner").SetValue(tinder,owner);if(upgraded)tinder.UpgradeInternal();
    ((Task)AccessTools.Method(typeof(WylderTinder),"OnPlay").Invoke(tinder,new object?[]{null,TrackingArrowRegression.MakePlay(tinder,owner)})!).GetAwaiter().GetResult();
    Check(Transformed is WylderFireCoating && Transformed.IsUpgraded==upgraded && Transformed.Owner==owner,"Tinder actual replacement upgrade and owner");
   }
   var power=(WylderMultiplayerPower)ModelDb.Power<WylderMultiplayerPower>().ToMutable();AccessTools.Property(typeof(PowerModel),"Owner").SetValue(power,owner.Creature);power.SetAmount(2,true);power.UpgradedCopies=1;((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(owner.Creature)!).Add(power);
   var card=ModelDb.Card<WylderGrappleShot>().ToMutable();AccessTools.Field(typeof(CardModel),"_owner").SetValue(card,owner);var play=TrackingArrowRegression.MakePlay(card,owner);
   WylderCombatEvents.RegisterBreak(play);WylderCombatEvents.RegisterBreak(play);
   var ledger=AccessTools.Field(typeof(WylderCombatEvents),"Plays").GetValue(null)!;object?[] values={play,null};ledger.GetType().GetMethod("TryGetValue")!.Invoke(ledger,values);var queue=(Queue<Func<PlayerChoiceContext,Task>>)AccessTools.Field(values[1]!.GetType(),"Rewards").GetValue(values[1])!;
   Check(queue.Count==1,"Duplicate callback queues once");while(queue.TryDequeue(out var action))action(null!).GetAwaiter().GetResult();
   Check(Generated.Count==4&&Generated.All(c=>c.Owner!=owner),"Two copies per other player, no self");
   foreach(var ally in new[]{ally1,ally2})Check(Generated.Count(c=>c.Owner==ally&&!c.IsUpgraded)==1&&Generated.Count(c=>c.Owner==ally&&c.IsUpgraded)==1,"Mixed normal/upgraded copies preserved");
   WylderCombatEvents.RegisterBreak(play);Check(queue.Count==0,"Already-resolved Break cannot reissue");
   var instinct=(WylderOffensiveInstinctPower)ModelDb.Power<WylderOffensiveInstinctPower>().ToMutable();
   AccessTools.Property(typeof(PowerModel),"Owner").SetValue(instinct,owner.Creature);instinct.SetAmount(2,true);
   ((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(owner.Creature)!).Add(instinct);
   WylderCombatEvents.RegisterEntry(null!,owner.Creature,card,WylderEffectSource.CardEffect).GetAwaiter().GetResult();
   Check(Applied.Count==0,"Entry with no enemies safely skips");
   var enemy=new Creature(ally1,50,50){CombatState=combat};AccessTools.Field(typeof(Creature),"<Side>k__BackingField").SetValue(enemy,CombatSide.Enemy);
   ((List<Creature>)AccessTools.Field(typeof(CombatState),"_enemies").GetValue(combat)!).Add(enemy);
   WylderCombatEvents.RegisterEntry(null!,owner.Creature,card,WylderEffectSource.CardEffect).GetAwaiter().GetResult();
   Check(Applied.Count==2&&Applied.All(a=>a.target==enemy&&a.amount==1&&a.power is MegaCrit.Sts2.Core.Models.Powers.VulnerablePower),"Each Instinct copy targets a living enemy once");
   WylderCombatEvents.RegisterEntry(null!,owner.Creature,card,WylderEffectSource.PowerEffect).GetAwaiter().GetResult();
   Check(Applied.Count==4,"Subsequent and power-sourced entries trigger");
   Generated.Clear();
   var lifePower=(WylderLifeFromDeathPower)ModelDb.Power<WylderLifeFromDeathPower>().ToMutable();
   AccessTools.Property(typeof(PowerModel),"Owner").SetValue(lifePower,owner.Creature);lifePower.SetAmount(2,true);
   lifePower.PendingBasic=2;lifePower.PendingUpgraded=2;
   lifePower.AfterPlayerTurnStartLate(null!,owner).GetAwaiter().GetResult();Check(Generated.Count==0,"Life cannot pay before next turn boundary");
   lifePower.AfterSideTurnStart(owner.Creature.Side,new[]{owner.Creature},combat).GetAwaiter().GetResult();
   lifePower.AfterPlayerTurnStartLate(null!,ally1).GetAwaiter().GetResult();Check(Generated.Count==0,"Life ignores teammate turn");
   lifePower.AfterPlayerTurnStartLate(null!,owner).GetAwaiter().GetResult();
   Check(Generated.Count==4&&Generated.All(c=>c is WylderDesperationCard&&c.Owner==owner)&&Generated.Count(c=>c.IsUpgraded)==2,"Life actually generates two basic and two upgraded Desperation cards for mixed copies");
   lifePower.AfterSideTurnStart(owner.Creature.Side,new[]{owner.Creature},combat).GetAwaiter().GetResult();
   lifePower.AfterPlayerTurnStartLate(null!,owner).GetAwaiter().GetResult();
   Check(Generated.Count==4&&lifePower.ModifyHandDraw(owner,5)==3,"Life only generates once but draw reduction persists");
   Console.WriteLine("PASS: Life from Death actual generated cards, upgraded mix, correct owner, next-turn only and lasting draw penalty.");
  } finally {h.UnpatchAll(h.Id);}
  Console.WriteLine("PASS: six-card metadata/upgrades, multiplayer-only pool, per-ally ownership, mixed upgrades duplicate Break guard and repeated Pursuit entries.");
 }
 public static bool Combat(CardModel __instance,ref ICombatState __result){__result=__instance.Owner.Creature.CombatState!;return false;}
 public static bool Select(ref Task<IEnumerable<CardModel>> __result){__result=Task.FromResult<IEnumerable<CardModel>>(new[]{Selected!});return false;}
 public static bool Transform(CardModel original,CardModel replacement,ref Task<CardPileAddResult?> __result){Check(original==Selected,"Tinder selected card replaced");Transformed=replacement;__result=Task.FromResult<CardPileAddResult?>(new CardPileAddResult{success=true,cardAdded=replacement});return false;}
 public static bool Apply(PowerModel power,Creature target,decimal amount,ref Task __result){Applied.Add((power,target,amount));AccessTools.Property(typeof(PowerModel),"Owner").SetValue(power,target);__result=Task.CompletedTask;return false;}
 public static bool Create(CardModel canonicalCard,Player owner,ref CardModel __result){__result=canonicalCard.ToMutable();AccessTools.Field(typeof(CardModel),"_owner").SetValue(__result,owner);return false;}
 public static bool Upgrade(CardModel card){card.UpgradeInternal();return false;}
 public static bool Add(CardModel __0,PileType __1,Player __2,ref Task<CardPileAddResult> __result){Check(__1==PileType.Hand&&__0.Owner==__2,"Generated hand destination and owner");Generated.Add(__0);__result=Task.FromResult(new CardPileAddResult{success=true,cardAdded=__0});return false;}
}

