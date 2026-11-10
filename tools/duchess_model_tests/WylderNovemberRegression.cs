using System.Reflection;
using System.Text.Json;
using SmartFormat;
using SmartFormat.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using NightMustStay.Core.Models.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Patches;

internal static partial class WylderBattleArtRegression
{
 private static CardModel? _payment;
 private static readonly List<string> _sequence = new();
 private static void NovemberCards()
 {
  int start=_checks;
  NovemberTemporaryStatPowerIcons();
  NovemberMultiplayer();
  NovemberText();
  foreach(bool up in new[]{false,true})
  {
   Fixture(); var flurry=Card(ModelDb.Card<WylderDesperateFlurry>(),up); Play(flurry);
   Check(flurry is WylderDesperationCard && flurry.EnergyCost.GetResolved()==3 && flurry.Rarity==CardRarity.Rare,"Flurry metadata");
   Check(Attacks.Single()==(false,3,up?11m:9m)&&ResolvedDamage.Count==3,"Flurry native three hits");
   Fixture();var end=Card(ModelDb.Card<WylderEndTheNight>(),up);Play(end);
   Check(end.Keywords.Contains(CardKeyword.Exhaust)&&end.EnergyCost.GetResolved()==5&&ResolvedDamage.Single()==(up?80:60),"End Night damage and exhaust");
   foreach(int extra in new[]{0,up?14:18,up?15:19,30})
   {
    Fixture();_fullyBlocked=true;AddPower<StrengthPower>(_owner.Creature,extra);
    var sword=Card(ModelDb.Card<WylderFlawlessSword>(),up);Play(sword);
    Check(Rewards.Where(r=>r.Kind=="Block").Sum(r=>r.Amount)==((up?15:11)+extra>=30?30:0),"Flawless pre-block threshold 29/30 and higher");
   }
   Fixture();var flame=Card(ModelDb.Card<WylderTrueFlame>(),up);var copy=Card(ModelDb.Card<WylderTrueFlame>(),up);int cost=up?4:5;Put(flame,PileType.Hand);
   Check(!flame.TryModifyEnergyCostInCombat(flame,cost,out var price)&&price==cost,"True Flame initial cost");
   for(int n=1;n<=6;n++)
   {
    WylderCombatEvents.RegisterEntry(null!,_owner.Creature,null!,WylderEffectSource.PowerEffect).GetAwaiter().GetResult();
    Check(flame.TryModifyEnergyCostInCombat(flame,cost,out price)&&price==Math.Max(0,cost-n),"True Flame combat entry discount clamps zero");
    Check(flame.EnergyCost.GetResolved()==Math.Max(0,cost-n),"True Flame native displayed and paid energy cost");
   }
   Check(flame.DynamicVars["Vigor"].BaseValue==20,"True Flame preview is 20 Vigor");
   Play(flame);Put(flame,PileType.Hand);Check(Rewards.Any(r=>r.Kind==nameof(VigorPower)&&r.Amount==20),"True Flame grants 20 Vigor");
   Check(!flame.TryModifyEnergyCostInCombat(flame,cost,out price)&&price==cost,"True Flame resets after play");
   Check(copy.TryModifyEnergyCostInCombat(copy,cost,out price)&&price==0,"True Flame copies independent");
   WylderCombatEvents.RegisterEntry(null!,_owner.Creature,null!,WylderEffectSource.PowerEffect).GetAwaiter().GetResult();
   Check(flame.TryModifyEnergyCostInCombat(flame,cost,out price)&&price==cost-1,"True Flame discount rebuilds after play");
   Check(!flame.TryModifyEnergyCostInCombat(copy,cost,out _),"True Flame only discounts itself");
   foreach(int handCount in new[]{0,3,4})
   {
    Fixture();AddPower<WylderForgetTheSwordPower>(_owner.Creature,up?50:25);
    for(int n=0;n<handCount;n++)Put(Card(ModelDb.Card<WylderDefend>(),false),PileType.Hand);
    var strike=Card(ModelDb.Card<WylderStrike>(),false);strike.DynamicVars.Damage.UpdateCardPreview(strike,CardPreviewMode.Normal,_enemies[0],true);
    Check(strike.DynamicVars.Damage.PreviewValue==6m*(handCount<=3?(up?1.5m:1.25m):1m),"Forget Sword current-hand damage preview");
    Play(strike);Check(ResolvedDamage.Single()==(handCount<=3?(up?9:7):6),"Forget Sword actual native damage multiplier");
   }
   Fixture();var aura=Card(ModelDb.Card<WylderMurderousAura>(),up);Play(aura);var power=_owner.Creature.GetPower<WylderMurderousAuraPower>()!;
   Check(power.ModifyHandDraw(_owner,5)==4&&power.ModifyHandDraw(_owner,0)==0,"Aura persistent draw penalty");
   Check(Rewards.Count(r=>r.Kind is nameof(StrengthPower) or nameof(DexterityPower))==2
      && Rewards.Any(r=>r.Kind==nameof(StrengthPower)&&r.Amount==(up?3:2))
      && Rewards.Any(r=>r.Kind==nameof(DexterityPower)&&r.Amount==(up?3:2))
      && Rewards.Any(r=>r.Kind==nameof(WylderMurderousAuraPower)&&r.Amount==1),
      "Aura grants Strength and Dexterity once on play and keeps only the draw-penalty power");
   Check(ModelDb.Card<WylderPassTheFlame>().MultiplayerConstraint==CardMultiplayerConstraint.MultiplayerOnly&&ModelDb.Card<WylderPassTheFlame>().TargetType==TargetType.AnyPlayer,"Pass Flame multiplayer target");
   var pass=Card(ModelDb.Card<WylderPassTheFlame>(),up);Check(pass.DynamicVars.Cards.IntValue==(up?3:2),"Pass Flame upgrade");
   var heroes=Card(ModelDb.Card<WylderHeroesAssemble>(),up);Check(heroes.EnergyCost.GetResolved()==(up?0:1)&&heroes.MultiplayerConstraint==CardMultiplayerConstraint.MultiplayerOnly,"Heroes metadata");
  }
  var h=new HarmonyLib.Harmony("wylder.november.payments");
  void Patch(MethodBase m,string p)=>h.Patch(m,prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),p));
  Patch(AccessTools.Method(typeof(CardSelectCmd),nameof(CardSelectCmd.FromHand)),nameof(NovemberSelect));
  Patch(AccessTools.Method(typeof(CardSelectCmd),nameof(CardSelectCmd.FromHandForDiscard)),nameof(NovemberSelect));
  Patch(AccessTools.Method(typeof(CardCmd),nameof(CardCmd.Exhaust)),nameof(NovemberExhaust));
  Patch(AccessTools.Method(typeof(CardCmd),nameof(CardCmd.Discard),new[]{typeof(PlayerChoiceContext),typeof(CardModel)}),nameof(NovemberDiscard));
  Patch(AccessTools.Method(typeof(PlayerCmd),nameof(PlayerCmd.EndTurn)),nameof(NovemberEnd));
  try
  {
   foreach(bool up in new[]{false,true})foreach(bool empty in new[]{false,true})
   foreach(var model in new CardModel[]{ModelDb.Card<WylderLoneCourage>(),ModelDb.Card<WylderAgility>(),ModelDb.Card<WylderHunt>(),ModelDb.Card<WylderVanish>()})
   {
    Fixture();_sequence.Clear();_payment=empty?null:Card(ModelDb.Card<WylderDefend>(),false);if(_payment!=null)Put(_payment,PileType.Hand);
    var card=Card(model,up);Play(card);
    Check(_sequence.SequenceEqual(empty?Array.Empty<string>():new[]{card is WylderLoneCourage or WylderHunt?"Exhaust":"Discard"}),"Hand selection moves exact chosen card");
    int amount=card is WylderLoneCourage or WylderAgility?(up?4:3):(up?3:2);
    Check(Rewards.Count==(card is WylderHunt or WylderVanish?2:1)&&Rewards.All(r=>r.Amount==amount),"Payment cards follow native empty-hand semantics and upgraded reward");
    Check(!card.Keywords.Contains(CardKeyword.Exhaust),"Exhaust chosen card does not exhaust source");
   }
   foreach(bool up in new[]{false,true})
   {
    Fixture();_sequence.Clear();var keep=Card(ModelDb.Card<WylderEndTheNight>(),false);Put(keep,PileType.Hand);Put(Card(ModelDb.Card<WylderStrike>(),false),PileType.Hand);Put(Card(ModelDb.Card<WylderDefend>(),false),PileType.Hand);
    var gamble=Card(ModelDb.Card<WylderGamble>(),up);Play(gamble);
    Check(Rewards.First().Kind=="Draw"&&Rewards.First().Amount==(up?6:4),"Gamble draws first");
    Check(_sequence.SequenceEqual(new[]{"Discard","Discard","End"})&&PileType.Hand.GetPile(_owner).Cards.SequenceEqual(new[]{keep})&&!keep.Keywords.Contains(CardKeyword.Retain),"Gamble retains only Desperation through turn end without making Retain permanent");
    Check(gamble.Keywords.Contains(CardKeyword.Exhaust)&&gamble.EnergyCost.GetResolved()==0,"Gamble Exhaust and free cost");
   }
  }finally{h.UnpatchAll(h.Id);}
  Console.WriteLine($"PASS: {_checks-start} new November card checks: damage, thresholds, hand selection, empty hand, temporary stats, draw penalty, discounts, copies, multiplayer metadata and turn-end order.");
 }

 private static void NovemberTemporaryStatPowerIcons()
 {
  var specs=new (PowerModel Power,string Id)[]
  {
   (ModelDb.Power<WylderValorStrengthPower>().ToMutable(),"wylder_valor_strength_power"),
   (ModelDb.Power<WylderLoneCouragePower>().ToMutable(),"wylder_lone_courage_power"),
   (ModelDb.Power<WylderAgilityPower>().ToMutable(),"wylder_agility_power"),
   (ModelDb.Power<WylderUppercutStrengthDownPower>().ToMutable(),"wylder_uppercut_strength_down_power")
  };
  foreach(var spec in specs)
  {
   string resolved="missing";
   Check(!WylderAssetPatch.ResolveWylderTemporaryStatPowerBigIconPath(spec.Power,ref resolved)
    && resolved==$"res://images/powers/{spec.Id}.png","Temporary stat big/flash icon is explicitly resolved "+spec.Id);
   Check(File.Exists($"images/powers/{spec.Id}.png")
    && File.Exists($"powers/{spec.Id}.png")
    && File.Exists($"images/atlases/power_atlas.sprites/{spec.Id}.tres"),"Temporary stat compact and flash icon files exist "+spec.Id);
  }
  string untouched="unchanged";
  Check(WylderAssetPatch.ResolveWylderTemporaryStatPowerBigIconPath(ModelDb.Power<StrengthPower>().ToMutable(),ref untouched)
   && untouched=="unchanged","Native Strength icon resolution remains native");
 }


 private static void NovemberText()
 {
  var models=new CardModel[]{ModelDb.Card<WylderDesperateFlurry>(),ModelDb.Card<WylderHeroesAssemble>(),ModelDb.Card<WylderPassTheFlame>(),ModelDb.Card<WylderMurderousAura>(),ModelDb.Card<WylderGamble>(),ModelDb.Card<WylderForgetTheSword>(),ModelDb.Card<WylderLoneCourage>(),ModelDb.Card<WylderAgility>(),ModelDb.Card<WylderEndTheNight>(),ModelDb.Card<WylderTrueFlame>(),ModelDb.Card<WylderFlawlessSword>(),ModelDb.Card<WylderHunt>(),ModelDb.Card<WylderVanish>()};
  var formatter=new SmartFormatter();formatter.AddExtensions(new DictionarySource(),new ReflectionSource(),new DefaultSource());formatter.AddExtensions(new HighlightDifferencesFormatter(),new ShowIfUpgradedFormatter());
  foreach(var locale in new[]{"zhs","eng","jpn","kor"})
  {
   using var json=JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
   foreach(var model in models)
   {
    Fixture();var text=json.RootElement.GetProperty(model.Id.Entry+".description").GetString()!;
    string Render(bool up){var card=Card(model,up);return formatter.Format(System.Globalization.CultureInfo.InvariantCulture,text,card.DynamicVars.ToDictionary(p=>p.Key,p=>(object)p.Value));}
    Check(!Render(false).Contains('{')&&!Render(true).Contains('{'),"New card base and upgraded text fully formats "+model.Id);
    Check(text==json.RootElement.GetProperty(model.Id.Entry+".upgradeDescription").GetString(),"New card upgrade audit copy synced "+model.Id);
    Check((Render(false)!=Render(true))==(model is not WylderHeroesAssemble and not WylderTrueFlame),"Visible upgraded values, or native cost upgrade "+model.Id);
    var card=Card(model,false);Check(card.TargetType==(card is WylderPassTheFlame?TargetType.AnyPlayer:card.Type==CardType.Attack?TargetType.AnyEnemy:TargetType.Self),"New card target "+card.Id);
   }
  }
 }

 private static readonly List<CardModel> _offered = new();
 private static Player NovemberAlly()
 {
  var p=TrackingArrowRegression.MakePlayer();AccessTools.Field(typeof(Player),"<Character>k__BackingField").SetValue(p,ModelDb.Character<Wylder>());AccessTools.Field(typeof(Player),"_runState").SetValue(p,NullRunState.Instance);
  var cr=new Creature(p,1000,1000){CombatState=_combat};AccessTools.Field(typeof(Player),"<Creature>k__BackingField").SetValue(p,cr);((List<Creature>)AccessTools.Field(typeof(CombatState),"_allies").GetValue(_combat)!).Add(cr);return p;
 }
 private static void NovemberMove(CardModel card,PileType dest,bool top=false)
 {
  foreach(var type in new[]{PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,PileType.Play})((List<CardModel>)AccessTools.Field(typeof(CardPile),"_cards").GetValue(type.GetPile(card.Owner))!).Remove(card);
  var cards=(List<CardModel>)AccessTools.Field(typeof(CardPile),"_cards").GetValue(dest.GetPile(card.Owner))!;if(top)cards.Insert(0,card);else cards.Add(card);
 }
 private static void NovemberMultiplayer()
 {
  var h=new HarmonyLib.Harmony("wylder.november.multiplayer");
  void Patch(MethodBase m,string p)=>h.Patch(m,prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),p){priority=Priority.First});
  Patch(typeof(CardPileCmd).GetMethods().Single(m=>m.Name=="Add"&&m.GetParameters()[0].ParameterType==typeof(CardModel)&&m.GetParameters()[1].ParameterType==typeof(PileType)),nameof(NovemberAdd));
  Patch(typeof(CardPileCmd).GetMethods().Single(m=>m.Name=="Draw"&&m.GetParameters().Length==4),nameof(NovemberDraw));
  Patch(typeof(CardSelectCmd).GetMethods().Single(m=>m.Name=="FromCombatPile"&&m.GetParameters().Length==5),nameof(NovemberOffer));
  try
  {
   foreach(bool up in new[]{false,true})foreach(int attackCount in new[]{0,1,5})
   {
    Fixture();var ally=NovemberAlly();var expected=new List<CardModel>();
    for(int i=0;i<attackCount;i++){var c=Card(ModelDb.Card<WylderStrike>(),false);AccessTools.Field(typeof(CardModel),"_owner").SetValue(c,ally);NovemberMove(c,PileType.Draw);expected.Add(c);}
    var skill=Card(ModelDb.Card<WylderDefend>(),false);AccessTools.Field(typeof(CardModel),"_owner").SetValue(skill,ally);NovemberMove(skill,PileType.Draw);
    var discarded=Card(ModelDb.Card<WylderStrike>(),false);AccessTools.Field(typeof(CardModel),"_owner").SetValue(discarded,ally);NovemberMove(discarded,PileType.Discard);
    Play(Card(ModelDb.Card<WylderPassTheFlame>(),up),target:ally.Creature);
    var hand=PileType.Hand.GetPile(ally).Cards;Check(hand.Count==Math.Min(attackCount,up?3:2)&&hand.All(expected.Contains)&&hand.Distinct().Count()==hand.Count,"Pass Flame actual random attacks, selected teammate, no duplicates");
    Check(skill.Pile!.Type==PileType.Draw&&discarded.Pile!.Type==PileType.Discard&&PileType.Hand.GetPile(_owner).Cards.Count==0,"Pass Flame ignores skills, discard pile and caster");
   }
   foreach(int attackCount in new[]{0,1,2,3,7})
   {
    Fixture();_offered.Clear();for(int i=0;i<attackCount;i++)Put(Card(ModelDb.Card<WylderStrike>(),false),PileType.Draw);Put(Card(ModelDb.Card<WylderDefend>(),false),PileType.Draw);
    var mastery=AddPower<WylderMartialMasteryPower>(_owner.Creature,1);mastery.AfterPlayerTurnStartLate(null!,_owner).GetAwaiter().GetResult();
    Check(_offered.Count==Math.Min(3,attackCount)&&_offered.All(c=>c.Type==CardType.Attack)&&_offered.Distinct().Count()==_offered.Count,"Mastery offers at most three distinct random attacks");
    Check(PileType.Hand.GetPile(_owner).Cards.Count==(attackCount>0?1:0),"Mastery selected one actual card moves to hand");
   }
   Fixture();var alive=NovemberAlly();var dead=NovemberAlly();dead.Creature.SetCurrentHpInternal(0);
   Play(Card(ModelDb.Card<WylderHeroesAssemble>(),false));var heroes=_owner.Creature.GetPower<WylderHeroesAssemblePower>()!;
   heroes.AfterPlayerTurnStartLate(null!,_owner).GetAwaiter().GetResult();Check(!_owner.Creature.HasPower<WylderPursuitPower>(),"Heroes not on same start phase");
   heroes.AfterSideTurnStart(_owner.Creature.Side,new[]{_owner.Creature},_combat).GetAwaiter().GetResult();
   heroes.AfterPlayerTurnStartLate(null!,alive).GetAwaiter().GetResult();Check(!alive.Creature.HasPower<WylderPursuitPower>(),"Heroes ignores teammate's trigger");
   heroes.AfterPlayerTurnStartLate(null!,_owner).GetAwaiter().GetResult();
   Check(_owner.Creature.HasPower<WylderPursuitPower>()&&alive.Creature.HasPower<WylderPursuitPower>()&&!dead.Creature.HasPower<WylderPursuitPower>(),"Heroes enters Pursuit for all living players including self");
   Check(!_owner.Creature.HasPower<WylderHeroesAssemblePower>(),"Heroes scheduled effect consumed once");
  }finally{h.UnpatchAll(h.Id);}
 }
 public static bool NovemberAdd(CardModel card,PileType newPileType,CardPilePosition position,ref Task<CardPileAddResult> __result){NovemberMove(card,newPileType,position==CardPilePosition.Top);__result=Task.FromResult(new CardPileAddResult{success=true,cardAdded=card});return false;}
 public static bool NovemberDraw(decimal count,Player player,ref Task<IEnumerable<CardModel>> __result){var result=new List<CardModel>();for(int i=0;i<count;i++){var c=PileType.Draw.GetPile(player).Cards.FirstOrDefault();if(c==null)break;NovemberMove(c,PileType.Hand);result.Add(c);}__result=Task.FromResult<IEnumerable<CardModel>>(result);return false;}
 public static bool NovemberOffer(CardPile pile,Func<CardModel,bool> filter,ref Task<IEnumerable<CardModel>> __result){_offered.Clear();_offered.AddRange(pile.Cards.Where(filter));__result=Task.FromResult<IEnumerable<CardModel>>(_offered.Take(1).ToArray());return false;}

 public static bool NovemberSelect(Func<CardModel,bool>? filter,ref Task<IEnumerable<CardModel>> __result){__result=Task.FromResult<IEnumerable<CardModel>>(_payment==null||(filter!=null&&!filter(_payment))?Array.Empty<CardModel>():new[]{_payment});return false;}
 public static bool NovemberExhaust(CardModel card,ref Task __result){Check(card==_payment,"Exhaust selection");_sequence.Add("Exhaust");Put(card,PileType.Exhaust);__result=Task.CompletedTask;return false;}
 public static bool NovemberDiscard(CardModel card,ref Task __result){_sequence.Add("Discard");Put(card,PileType.Discard);__result=Task.CompletedTask;return false;}
 public static bool NovemberEnd(Func<Task>? __2){_sequence.Add("End");__2?.Invoke().GetAwaiter().GetResult();return false;}
}
