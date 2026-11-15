using HarmonyLib;
using System.Text.Json;
using SmartFormat;
using SmartFormat.Extensions;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
internal static partial class WylderBattleArtRegression
{
 private static void VortexCards()
 {
  int start=_checks;
  var formatter=new SmartFormatter();formatter.AddExtensions(new DictionarySource(),new ReflectionSource(),new DefaultSource());formatter.AddExtensions(new HighlightDifferencesFormatter());
  foreach(var locale in new[]{"zhs","eng","jpn","kor"})foreach(var model in new CardModel[]{ModelDb.Card<WylderHorizontalSlash>(),ModelDb.Card<WylderVerticalCleave>(),ModelDb.Card<WylderOrdovisVortex>(),ModelDb.Card<WylderSiluriaVortex>()})
  {
   using var json=JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));var text=json.RootElement.GetProperty(model.Id.Entry+".description").GetString()!;
   string Render(bool up){Fixture();var card=Card(model,up);if(card is WylderSiluriaVortex)card.DynamicVars.Damage.UpdateCardPreview(card,CardPreviewMode.Normal,_enemies[0],true);return formatter.Format(System.Globalization.CultureInfo.InvariantCulture,text,card.DynamicVars.ToDictionary(p=>p.Key,p=>(object)p.Value));}
   Check((model is WylderSiluriaVortex ? Render(false)==Render(true) : Render(false)!=Render(true))&&!Render(false).Contains('{'),"Vortex batch four-locale upgrade text");Check(text==json.RootElement.GetProperty(model.Id.Entry+".upgradeDescription").GetString(),"Vortex batch audit text synced");
  }

  foreach(bool up in new[]{false,true})
  {
   foreach(var model in new CardModel[]{ModelDb.Card<WylderHorizontalSlash>(),ModelDb.Card<WylderVerticalCleave>(),ModelDb.Card<WylderOrdovisVortex>(),ModelDb.Card<WylderSiluriaVortex>()})
   {
    Fixture();var card=Card(model,up);Check(card.Type==CardType.Attack&&card.TargetType==TargetType.AnyEnemy,"Vortex batch attack targets");Check(card.EnergyCost.GetResolved()==(card is WylderSiluriaVortex?(up?2:3):card is WylderVerticalCleave?2:1),"Vortex batch costs");
    Check(card.Rarity==(card is WylderOrdovisVortex or WylderSiluriaVortex?CardRarity.Rare:CardRarity.Common),"Vortex batch rarity");
   }
   foreach(int strength in new[]{0,up?6:9,up?7:10})
   {
    Fixture();if(strength>0)AddPower<StrengthPower>(_owner.Creature,strength);var card=Card(ModelDb.Card<WylderHorizontalSlash>(),up);Play(card);
    Check(ResolvedDamage.Single()==(up?13:10)+strength,"Horizontal Slash damage upgrade");Check(_owner.Creature.HasPower<WylderPursuitPower>()==((up?13:10)+strength>=20),"Horizontal Slash exact Break20 enters Pursuit");
   }
   Fixture(true);AddPower<StrengthPower>(_owner.Creature,4);Play(Card(ModelDb.Card<WylderHorizontalSlash>(),up));Check(_owner.Creature.HasPower<WylderPursuitPower>(),"Horizontal Slash fresh entry survives prior Pursuit cleanup");
   Fixture();var vertical=Card(ModelDb.Card<WylderVerticalCleave>(),up);Play(vertical);Check(vertical is WylderFollowThroughCard&&ResolvedDamage.Single()==(up?16:12),"Vertical Cleave Follow Through and damage");
   foreach(int count in new[]{0,1,3})
   {
    Fixture(true);AddPower<StrengthPower>(_owner.Creature,2);
    var type=typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderStunHistory")!;
    for(int i=0;i<count;i++)AccessTools.Method(type,"Record").Invoke(null,new object[]{_owner});
    var card=Card(ModelDb.Card<WylderSiluriaVortex>(),up);Put(card,PileType.Hand);int baseCost=up?2:3;
    Check((count>0)==card.TryModifyEnergyCostInCombat(card,baseCost,out var discounted)&&discounted==Math.Max(0,baseCost-count),"Siluria stun-count cost preview");
    Check(card.EnergyCost.GetResolved()==Math.Max(0,baseCost-count),"Siluria displayed and paid energy cost");
    card.DynamicVars.Damage.UpdateCardPreview(card,CardPreviewMode.Normal,_enemies[0],true);
    decimal expected=(24+2)*1.5m;
    Check(card.DynamicVars.Damage.PreviewValue==expected,"Siluria fixed damage preview with Strength and Pursuit");
    Play(card);Check(ResolvedDamage.Single()==Math.Floor(expected),"Siluria actual damage equals preview");
    Check(!card.DynamicVars.Damage.WasJustUpgraded,"Siluria upgrade changes cost only");
    var replay=Card(ModelDb.Card<WylderSiluriaVortex>(),up);Put(replay,PileType.Hand);
    bool retainedDiscount=replay.TryModifyEnergyCostInCombat(replay,baseCost,out discounted);
    Check((count>0)==retainedDiscount&&discounted==Math.Max(0,baseCost-count),$"Siluria keeps the combat-wide stun discount after play (count={count}, changed={retainedDiscount}, cost={discounted})");
    AccessTools.Method(type,"Record").Invoke(null,new object[]{_owner});
    Check(replay.TryModifyEnergyCostInCombat(replay,baseCost,out discounted)&&discounted==Math.Max(0,baseCost-count-1),"Siluria adds another discount after another stun");
   }
   Fixture();var veteran=Card(ModelDb.Card<WylderVeteranEdge>(),up);Check(veteran.DynamicVars["Strength"].BaseValue==2,"Veteran Edge base/upgraded Strength2");
   AddPower<WylderVeteranEdgePower>(_owner.Creature,1);AddPower<StrengthPower>(_owner.Creature,10);Play(Card(ModelDb.Card<WylderHorizontalSlash>(),false));
   WylderCombatEvents.CompleteCard(null!,MegaCrit.Sts2.Core.Combat.CombatManager.Instance.History.CardPlaysStarted.Last().CardPlay).GetAwaiter().GetResult();
   Check(Rewards.Any(r=>r.Kind==nameof(StrengthPower)&&r.Amount==2),"Veteran Edge actual Break reward2");
  }
  Check(ModelDb.Card<WylderCriticalStrike>().Rarity==CardRarity.Uncommon&&ModelDb.Card<WylderCounterRush>().Rarity==CardRarity.Common,"Critical Strike Uncommon and Recoil Stake Common");
  Console.WriteLine($"PASS: {_checks-start} Vortex batch checks: targets, rarity, costs, Break entry, Follow Through, stun growth and native preview.");
 }
}
