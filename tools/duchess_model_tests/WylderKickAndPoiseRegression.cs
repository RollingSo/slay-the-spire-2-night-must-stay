using NightMustStay.Core.Models.Power;
using HarmonyLib;
using System.Text.Json;
using SmartFormat;
using SmartFormat.Extensions;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;
internal static partial class WylderBattleArtRegression
{
 private static bool KickDenyStun;
 private static void KickAndPoiseCards()
 {
  int start=_checks;
  if(!ModelDb.Contains(typeof(BigDummy)))AccessTools.Method(typeof(ModelDb),"Inject").Invoke(null,new object[]{typeof(BigDummy)});
  var formatter=new SmartFormatter();formatter.AddExtensions(new DictionarySource(),new ReflectionSource(),new DefaultSource());formatter.AddExtensions(new HighlightDifferencesFormatter(),new EnergyIconsFormatter());
  foreach(var locale in new[]{"zhs","eng","jpn","kor"})foreach(var model in new CardModel[]{ModelDb.Card<WylderKick>(),ModelDb.Card<WylderPoise>()})
  {
   using var json=JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));var text=json.RootElement.GetProperty(model.Id.Entry+".description").GetString()!;
   string Render(bool up){Fixture();var card=Card(model,up);Hand(card,1);foreach(var energy in card.DynamicVars.Values.OfType<EnergyVar>())energy.ColorPrefix=EnergyIconHelper.GetPrefix(card);return formatter.Format(System.Globalization.CultureInfo.InvariantCulture,text,card.DynamicVars.ToDictionary(p=>p.Key,p=>(object)p.Value));}
   Check(Render(false)!=Render(true)&&!Render(true).Contains('{'),"Kick/Poise rendered upgrades in "+locale);
   Check(text==json.RootElement.GetProperty(model.Id.Entry+".upgradeDescription").GetString(),"Kick/Poise upgrade audit copy "+locale);
  }
  var h=new HarmonyLib.Harmony("wylder.kick.poise");
  h.Patch(AccessTools.Method(typeof(CreatureCmd),nameof(CreatureCmd.Stun),new[]{typeof(Creature),typeof(string)}),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(KickStun)){priority=Priority.First});
  try
  {
   foreach(bool up in new[]{false,true})foreach(int stacks in new[]{0,1,2})foreach(bool deny in new[]{false,true})
   {
    Fixture();KickDenyStun=deny;
    ((List<Creature>)AccessTools.Field(typeof(MegaCrit.Sts2.Core.Combat.CombatState),"_enemies").GetValue(_combat)!).Clear();
    var target=(Creature)AccessTools.Method(typeof(WylderStunReturnRegression),"Enemy").Invoke(null,new object[]{MegaCrit.Sts2.Core.Combat.CombatSide.Enemy})!;
    if(stacks>0)AddPower<PhantomImbalancePower>(target,stacks);
    var card=Card(ModelDb.Card<WylderKick>(),up);Hand(card,1);
    bool shouldGlow=stacks+(up?2:1)>=PhantomImbalancePower.GetThreshold(target);
    Check(((bool)AccessTools.PropertyGetter(typeof(WylderCard),"ShouldGlowGoldInternal").Invoke(card,null)!)==shouldGlow,"Kick glow follows potential Stun threshold");
    Play(card,target:target);
    bool refund=!deny&&stacks+(up?2:1)>=PhantomImbalancePower.GetThreshold(1);
    Check(card.Type==CardType.Skill&&card.Rarity==CardRarity.Uncommon&&card.TargetType==TargetType.AnyEnemy&&card.EnergyCost.GetResolved()==0,"Kick metadata");
    Check(Rewards.Where(r=>r.Kind=="Energy").Sum(r=>r.Amount)==(refund?1:0)&&Rewards.Where(r=>r.Kind=="Draw").Sum(r=>r.Amount)==(refund?1:0),$"Kick refund up={up}, stacks={stacks}, deny={deny}, stunned={target.IsStunned}, alive={target.IsAlive}, rewards={string.Join(",",Rewards.Select(r=>r.Kind+":"+r.Amount))}");
    Check(target.IsStunned==refund,"Kick native enemy Stun state");
   }
   foreach(bool up in new[]{false,true})foreach(int count in new[]{1,2,3,4})foreach(int dex in new[]{0,3})
   {
    Fixture();if(dex>0)AddPower<DexterityPower>(_owner.Creature,dex);
    var card=Card(ModelDb.Card<WylderPoise>(),up);Hand(card,count);
    card.DynamicVars.Block.UpdateCardPreview(card,CardPreviewMode.Normal,null!,true);
    int shown=(up?20:16)+dex;
    int expected=count-1<=2?shown:0;
    Check(card.DynamicVars.Block.PreviewValue==shown,"Poise always displays its nonzero Block value");
    Check(((bool)AccessTools.PropertyGetter(typeof(WylderCard),"ShouldGlowGoldInternal").Invoke(card,null)!)==(expected>0),"Poise glow uses post-play hand size at most two");
    Check(card.Keywords.Contains(CardKeyword.Retain)&&card.EnergyCost.GetResolved()==1&&card.Rarity==CardRarity.Uncommon&&card.TargetType==TargetType.Self,"Poise native Retain and metadata");
    Play(card);Check(Rewards.Where(r=>r.Kind=="Block").Sum(r=>r.Amount)==expected,"Poise actual Block uses pre-move hand count");
   }
   Fixture();var notHand=Card(ModelDb.Card<WylderPoise>(),true);Play(notHand);Check(!Rewards.Any(r=>r.Kind=="Block"),"Poise replay/non-hand use cannot inherit low-hand qualification");
  }finally{h.UnpatchAll(h.Id);}
  Console.WriteLine($"PASS: {_checks-start} Kick/Poise checks: native Stun thresholds and glow, post-play hand boundary, Retain, upgrades, and nonzero Block previews.");
 }
 public static bool KickStun(Creature creature,ref Task __result){__result=KickDenyStun?Task.CompletedTask:CreatureCmd.Stun(creature,_=>Task.CompletedTask,"NOTHING");return false;}
}
