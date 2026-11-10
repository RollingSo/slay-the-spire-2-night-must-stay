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
   string Render(bool up){Fixture();var card=Card(model,up);if(card is WylderSiluriaVortex)card.DynamicVars.CalculatedDamage.UpdateCardPreview(card,CardPreviewMode.Normal,_enemies[0],true);return formatter.Format(System.Globalization.CultureInfo.InvariantCulture,text,card.DynamicVars.ToDictionary(p=>p.Key,p=>(object)p.Value));}
   Check(Render(false)!=Render(true)&&!Render(false).Contains('{'),"Vortex batch four-locale upgrade text");Check(text==json.RootElement.GetProperty(model.Id.Entry+".upgradeDescription").GetString(),"Vortex batch audit text synced");
  }

  foreach(bool up in new[]{false,true})
  {
   foreach(var model in new CardModel[]{ModelDb.Card<WylderHorizontalSlash>(),ModelDb.Card<WylderVerticalCleave>(),ModelDb.Card<WylderOrdovisVortex>(),ModelDb.Card<WylderSiluriaVortex>()})
   {
    Fixture();var card=Card(model,up);Check(card.Type==CardType.Attack&&card.TargetType==TargetType.AnyEnemy,"Vortex batch attack targets");Check(card.EnergyCost.GetResolved()==(card is WylderVerticalCleave or WylderSiluriaVortex?2:1),"Vortex batch costs");
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
    var card=Card(ModelDb.Card<WylderSiluriaVortex>(),up);card.DynamicVars.CalculatedDamage.UpdateCardPreview(card,CardPreviewMode.Normal,_enemies[0],true);
    decimal expected=(24+count*(up?10:5)+2)*1.5m;
    Check(card.DynamicVars.CalculatedDamage.PreviewValue==expected,"Siluria native dynamic preview with Strength and Pursuit");
    Play(card);Check(ResolvedDamage.Single()==Math.Floor(expected),"Siluria actual damage equals preview");
    Check(!card.DynamicVars.CalculationBase.WasJustUpgraded&&card.DynamicVars["ExtraDamage"].WasJustUpgraded==up,"Siluria only per-stun growth upgrades");
   }
   Fixture();var veteran=Card(ModelDb.Card<WylderVeteranEdge>(),up);Check(veteran.DynamicVars["Strength"].BaseValue==2,"Veteran Edge base/upgraded Strength2");
   AddPower<WylderVeteranEdgePower>(_owner.Creature,1);AddPower<StrengthPower>(_owner.Creature,10);Play(Card(ModelDb.Card<WylderHorizontalSlash>(),false));
   WylderCombatEvents.CompleteCard(null!,MegaCrit.Sts2.Core.Combat.CombatManager.Instance.History.CardPlaysStarted.Last().CardPlay).GetAwaiter().GetResult();
   Check(Rewards.Any(r=>r.Kind==nameof(StrengthPower)&&r.Amount==2),"Veteran Edge actual Break reward2");
  }
  Check(ModelDb.Card<WylderCriticalStrike>().Rarity==CardRarity.Uncommon&&ModelDb.Card<WylderCounterRush>().Rarity==CardRarity.Uncommon,"Critical Strike and Recoil Stake Uncommon");
  Console.WriteLine($"PASS: {_checks-start} Vortex batch checks: targets, rarity, costs, Break entry, Follow Through, stun growth and native preview.");
 }
}
