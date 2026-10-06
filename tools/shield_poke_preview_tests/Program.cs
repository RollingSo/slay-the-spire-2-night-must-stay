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
MethodInfo initModelDb = typeof(ModelDb).GetMethod("Init", flags)!;
object?[] initArgs = initModelDb.GetParameters()
    .Select(parameter => parameter.HasDefaultValue
        ? parameter.DefaultValue
        : parameter.ParameterType.IsValueType
            ? Activator.CreateInstance(parameter.ParameterType)
            : null)
    .ToArray();
initModelDb.Invoke(null, initArgs);
foreach (var type in new[] { typeof(ShieldPoke), typeof(Fearless), typeof(SpearGrinding),
    typeof(SpearPolish), typeof(Heavenfall), typeof(EveOfCounterattack), typeof(FinalCurtainHalberd),
    typeof(DuchessUndecidedFate) })
    if (!ModelDb.Contains(type)) typeof(ModelDb).GetMethod("Inject", flags)!.Invoke(null, [type]);

void Assert(bool value, string message) { if (!value) throw new Exception(message); }
void CheckCostUpgrade<T>(int normal, int upgraded) where T : CardModel
{
    var card = (T)ModelDb.Card<T>().MutableClone();
    Assert(card.EnergyCost.GetResolved() == normal, typeof(T).Name + " base cost.");
    typeof(T).GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(card, null);
    Assert(card.EnergyCost.GetResolved() == upgraded, typeof(T).Name + " upgraded cost.");
}
CheckCostUpgrade<Fearless>(1, 0);
CheckCostUpgrade<SpearGrinding>(1, 0);
Assert(ModelDb.Card<Fearless>().Keywords.Contains(CardKeyword.Retain), "Fearless must retain.");
Assert(ModelDb.Card<EveOfCounterattack>().EnergyCost.GetResolved() == 1, "Counterattack cost.");
Assert(ModelDb.Card<DuchessUndecidedFate>().Keywords.Contains(CardKeyword.Exhaust), "Fate must exhaust.");
var heavenfall = (Heavenfall)ModelDb.Card<Heavenfall>().MutableClone();
Assert(heavenfall is GuardianConcealedEdgeCard && heavenfall.Type == CardType.Attack
    && heavenfall.TargetType == TargetType.AnyEnemy && heavenfall.EnergyCost.GetResolved() == 10
    && heavenfall.DynamicVars.Damage.BaseValue == 100, "Heavenfall rules.");
typeof(Heavenfall).GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(heavenfall, null);
Assert(heavenfall.Keywords.Contains(CardKeyword.Retain), "Heavenfall upgrade must retain.");
var halberd = (FinalCurtainHalberd)ModelDb.Card<FinalCurtainHalberd>().MutableClone();
Assert(halberd.DynamicVars["Increase"].BaseValue == 12, "Halberd growth.");
typeof(FinalCurtainHalberd).GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(halberd, null);
Assert(halberd.DynamicVars["Increase"].BaseValue == 16, "Halberd upgraded growth.");
var harmony = new HarmonyLib.Harmony("NightMustStay.ShieldPoke.Preview.Tests");
// Stub only the existing history query: exercise real mutable card vars and both
// display paths, without needing a Godot combat room or altering gameplay history.
harmony.Patch(typeof(Fearless).GetMethod(nameof(Fearless.IsShieldPokeEmpowered))!,
    prefix: new HarmonyMethod(typeof(BonusFixture).GetMethod(nameof(BonusFixture.Prefix))!));
try
{
    harmony.CreateClassProcessor(typeof(Fearless).Assembly.GetType("NightMustStay.Core.Patches.FearlessDamageBranchPatch")!).Patch();
    var card = (ShieldPoke)ModelDb.Card<ShieldPoke>().MutableClone();
    void Preview(ShieldPoke poke, decimal damage, decimal block)
    {
        poke.DynamicVars.Damage.UpdateCardPreview(poke, CardPreviewMode.Normal, null!, false);
        poke.DynamicVars.Block.UpdateCardPreview(poke, CardPreviewMode.Normal, null!, false);
        Assert(poke.DynamicVars.Damage.PreviewValue==damage,"Wrong dynamic damage preview.");
        Assert(poke.DynamicVars.Block.PreviewValue==block,"Wrong dynamic block preview.");
        Assert((decimal)typeof(ShieldPoke).GetMethod("GetDamageBeforeHooks", BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(poke,null)! == poke.DynamicVars.Damage.BaseValue,
            "Raw attack must leave final doubling to the native multiplicative hook.");
    }
    Preview(card,3,3);
    BonusFixture.Empowered=true;
    var multiplierHook = typeof(CardModel).GetMethods().Single(method => method.Name == "ModifyDamageMultiplicative");
    var hookArgs = multiplierHook.GetParameters().Select(parameter => parameter.ParameterType == typeof(decimal)
        ? (object)14m : parameter.ParameterType == typeof(MegaCrit.Sts2.Core.ValueProps.ValueProp)
            ? MegaCrit.Sts2.Core.ValueProps.ValueProp.Move : parameter.ParameterType == typeof(CardModel) ? card : null).ToArray();
    Assert((decimal)multiplierHook.Invoke(card, hookArgs)! == 2m, "Native final damage multiplier must double damage after Strength.");
    Preview(card,6,0);
    Preview(card,6,0); // Repeated redraws must not compound transient damage.
    Assert(card.DynamicVars.Damage.BaseValue==3 && card.DynamicVars.Block.BaseValue==3,"Preview mutated base values.");
    var generated=(ShieldPoke)ModelDb.Card<ShieldPoke>().MutableClone();
    Preview(generated,6,0);
    Preview(card,6,0);
    typeof(ShieldPoke).GetMethod("OnUpgrade",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(card,null);
    Preview(card,10,0);
    BonusFixture.Empowered=false; // Next turn: no permanent bonus remains.
    Preview(card,5,5);
    Preview(generated,3,3);
    Assert(card.DynamicVars.Damage.Name=="Damage" && card.DynamicVars.Block.Name=="Block","Localization placeholders changed.");
}
finally { harmony.UnpatchAll(harmony.Id); }
Console.WriteLine("PASS: normal/upgraded/generated Shield Poke, Fearless doubling, repeated previews, turn reset, unchanged base values/keys.");

public static class BonusFixture
{
    public static bool Empowered;
    public static bool Prefix(ref bool __result) { __result=Empowered; return false; }
}
