using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
typeof(ModManager).GetMethod("ResetForTests", statics)!.Invoke(null, null);
var state = typeof(ModManager).GetProperty("State")!;
state.SetValue(null, Enum.Parse(state.PropertyType, "Skipped"));
typeof(ModelDb).GetMethod("Init", statics)!.Invoke(null, null);
foreach (var type in new[] { typeof(SaviorSpreadWings), typeof(SaviorSpreadWingsPower), typeof(IncomingDamageReductionThisTurnPower) })
    if (!ModelDb.Contains(type)) typeof(ModelDb).GetMethod("Inject", statics)!.Invoke(null, [type]);
Creature Creature(CombatSide side)
{
    var creature = new Creature((Player)RuntimeHelpers.GetUninitializedObject(typeof(Player)), 50, 50);
    typeof(Creature).GetField("<Side>k__BackingField", instance)!.SetValue(creature, side);
    return creature;
}
var guardian = Creature(CombatSide.Player);
var teammate = Creature(CombatSide.Player);
var enemy = Creature(CombatSide.Enemy);
var summonedEnemy = Creature(CombatSide.Enemy);
var power = (SaviorSpreadWingsPower)ModelDb.Power<SaviorSpreadWingsPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(power, guardian);
power.SetAmount(50, false);
var legacy = (IncomingDamageReductionThisTurnPower)ModelDb.Power<IncomingDamageReductionThisTurnPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(legacy, guardian);
legacy.SetAmount(50, false);
var harmony = new HarmonyLib.Harmony("NightMustStay.SaviorSpreadWings.Tests");
var assembly = typeof(SaviorSpreadWings).Assembly;
foreach (string name in new[] { "SaviorSpreadWingsDamageBranchPatch", "IncomingDamageReductionBranchPatch" })
    harmony.CreateClassProcessor(assembly.GetType("NightMustStay.Core.Patches." + name)!).Patch();
var hook = typeof(PowerModel).GetMethods(instance).Single(m => m.Name == "ModifyDamageMultiplicative");
decimal Multiplier(PowerModel model, Creature target, Creature? dealer, ValueProp props)
{
    object?[] args = hook.GetParameters().Select(p => p.Name switch
    {
        "target" => (object)target, "amount" => 20m, "props" => props, "dealer" => dealer, _ => null
    }).ToArray();
    return (decimal)hook.Invoke(model, args)!;
}
try
{
    foreach (var target in new[] { guardian, teammate })
    {
        Assert(Multiplier(power,target,enemy,ValueProp.Move)==.5m,"Enemy attack did not protect every player.");
        Assert(Multiplier(power,target,summonedEnemy,ValueProp.Move)==.5m,"Late summon bypassed aura.");
        Assert(Multiplier(power,target,enemy,ValueProp.Unpowered)==1m,"Non-attack damage was reduced.");
        Assert(Multiplier(power,target,null,ValueProp.Move)==1m,"Unattributed damage was reduced.");
    }
    Assert(Multiplier(power,enemy,guardian,ValueProp.Move)==1m,"Guardian attack was reduced.");
    Assert(Multiplier(power,enemy,teammate,ValueProp.Move)==1m,"Teammate attack was reduced.");
    power.SetAmount(75,false);
    Assert(Multiplier(power,teammate,enemy,ValueProp.Move)==.25m,"Upgrade is not 75 percent.");
    power.SetAmount(125,false);
    Assert(Multiplier(power,teammate,enemy,ValueProp.Move)==0m,"Stacked reduction produced negative damage.");
    Assert(Multiplier(legacy,guardian,enemy,ValueProp.Unpowered)==.5m,"Legacy self reduction changed.");
    Assert(Multiplier(legacy,teammate,enemy,ValueProp.Move)==1m,"Legacy reduction unexpectedly protects allies.");

    // Intercept only native removal to verify the real expiration hook without
    // requiring a Godot combat room. No turn condition is mocked.
    var remove = typeof(PowerCmd).GetMethods(statics).Single(m => m.Name=="Remove" && !m.IsGenericMethod &&
        m.GetParameters() is var p && p.Length==1 && p[0].ParameterType==typeof(PowerModel));
    harmony.Patch(remove, prefix:new HarmonyMethod(typeof(RemovalFixture).GetMethod(nameof(RemovalFixture.Prefix))!));
    await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),CombatSide.Player,[guardian,teammate]);
    Assert(RemovalFixture.Count==0,"Expired before enemy actions.");
    await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),CombatSide.Enemy,[enemy,summonedEnemy]);
    Assert(RemovalFixture.Count==1 && ReferenceEquals(RemovalFixture.Removed,power),"Did not expire after enemy turn.");

    var card = (SaviorSpreadWings)ModelDb.Card<SaviorSpreadWings>().MutableClone();
    Assert(card.DynamicVars.Damage.BaseValue==18 && card.DynamicVars["DamageReduction"].BaseValue==50,"Base values changed.");
    typeof(CardModel).GetMethod("UpgradeInternal",instance)!.Invoke(card,null);
    Assert(card.DynamicVars.Damage.BaseValue==22 && card.DynamicVars["DamageReduction"].BaseValue==75,"Upgrade values changed.");
    Assert(card.Keywords.Contains(CardKeyword.Exhaust),"Exhaust removed.");
    foreach (string locale in new[] { "zhs", "eng", "jpn" })
    {
        using var cards=JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
        using var powers=JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/powers.json"));
        var description=cards.RootElement.GetProperty("SAVIOR_SPREAD_WINGS.description").GetString()!;
        Assert(description==cards.RootElement.GetProperty("SAVIOR_SPREAD_WINGS.upgradeDescription").GetString(),"Upgrade text diverges.");
        Assert(description.Contains("{DamageReduction:diff()}"),"Dynamic percentage lost.");
        Assert(powers.RootElement.GetProperty("SAVIOR_SPREAD_WINGS_POWER.description").GetString()!.Contains("{Amount}"),"Missing status text.");
    }
}
finally { harmony.UnpatchAll(harmony.Id); }
Console.WriteLine("PASS: live damage hook protects self/teammates/late summons; attack-only filter; 50/75 percent; stack clamp; enemy-turn expiry; legacy unchanged; card upgrade and three locales.");

public static class RemovalFixture
{
    public static int Count;
    public static PowerModel? Removed;
    public static bool Prefix(PowerModel __0, ref Task __result)
    { Count++; Removed=__0; __result=Task.CompletedTask; return false; }
}
