using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Models.Cards;

BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
typeof(ModManager).GetMethod("ResetForTests", flags)!.Invoke(null, null);
var state = typeof(ModManager).GetProperty("State")!;
state.SetValue(null, Enum.Parse(state.PropertyType, "Skipped"));
typeof(ModelDb).GetMethod("Init", flags)!.Invoke(null, null);
foreach (var type in new[] { typeof(ShieldPoke), typeof(Fearless) })
    if (!ModelDb.Contains(type)) typeof(ModelDb).GetMethod("Inject", flags)!.Invoke(null, [type]);

void Assert(bool value, string message) { if (!value) throw new Exception(message); }
var formula = typeof(ShieldPoke).GetMethod("CalculateDamageBeforeHooks", flags)!;
foreach (var (baseDamage, bonus, stacks, expected) in new (decimal,decimal,decimal,decimal)[]
    { (4,0,0,4),(4,6,0,10),(6,6,0,12),(4,12,0,16),(4,6,1,20),(6,6,2,48),(4,0,1,8) })
    Assert((decimal)formula.Invoke(null,[baseDamage,bonus,stacks])! == expected,"Raw damage formula mismatch.");

var harmony = new HarmonyLib.Harmony("NightMustStay.ShieldPoke.Preview.Tests");
// Stub only the existing history query: exercise real mutable card vars and both
// display paths, without needing a Godot combat room or altering gameplay history.
harmony.Patch(typeof(Fearless).GetMethod(nameof(Fearless.GetShieldPokeDamageBonus))!,
    prefix: new HarmonyMethod(typeof(BonusFixture).GetMethod(nameof(BonusFixture.Prefix))!));
try
{
    var card = (ShieldPoke)ModelDb.Card<ShieldPoke>().MutableClone();
    void Preview(ShieldPoke poke, decimal damage, decimal block)
    {
        poke.DynamicVars.Damage.UpdateCardPreview(poke, CardPreviewMode.Normal, null!, false);
        poke.DynamicVars.Block.UpdateCardPreview(poke, CardPreviewMode.Normal, null!, false);
        Assert(poke.DynamicVars.Damage.PreviewValue==damage,"Wrong dynamic damage preview.");
        Assert(poke.DynamicVars.Block.PreviewValue==block,"Wrong dynamic block preview.");
        Assert((decimal)typeof(ShieldPoke).GetMethod("GetDamageBeforeHooks", BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(poke,null)! == damage,
            "Preview and actual raw attack diverge.");
    }
    Preview(card,4,4);
    BonusFixture.Bonus=6;
    Preview(card,10,0);
    Preview(card,10,0); // Repeated redraws must not compound transient damage.
    Assert(card.DynamicVars.Damage.BaseValue==4 && card.DynamicVars.Block.BaseValue==4,"Preview mutated base values.");
    var generated=(ShieldPoke)ModelDb.Card<ShieldPoke>().MutableClone();
    Preview(generated,10,0);
    BonusFixture.Bonus=12;
    Preview(card,16,0);
    typeof(ShieldPoke).GetMethod("OnUpgrade",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(card,null);
    Preview(card,18,0);
    BonusFixture.Bonus=0; // Next turn: no permanent bonus remains.
    Preview(card,6,6);
    Preview(generated,4,4);
    Assert(card.DynamicVars.Damage.Name=="Damage" && card.DynamicVars.Block.Name=="Block","Localization placeholders changed.");
}
finally { harmony.UnpatchAll(harmony.Id); }
Console.WriteLine("PASS: normal/upgraded/generated Shield Poke, stacked Fearless, repeated previews, turn reset, unchanged base values/keys, Spear Grinding raw formula.");

public static class BonusFixture
{
    public static decimal Bonus;
    public static bool Prefix(ref decimal __result) { __result=Bonus; return false; }
}
