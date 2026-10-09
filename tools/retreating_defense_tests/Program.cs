using System.Reflection;
using System.Runtime.CompilerServices;
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
const BindingFlags inst=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
const BindingFlags stat=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
TestMode.IsOn=true;
typeof(ModManager).GetMethod("ResetForTests",stat)!.Invoke(null,null);
var state=typeof(ModManager).GetProperty("State")!;
state.SetValue(null,Enum.Parse(state.PropertyType,"Skipped"));
var init=typeof(ModelDb).GetMethod("Init",stat)!;
init.Invoke(null,init.GetParameters().Length==0 ? null : new object[]{Array.Empty<Type>()});
foreach(var t in new[]{typeof(RetreatingDefense),typeof(RetreatingDefensePower),typeof(GuardCounterPower)})
 if(!ModelDb.Contains(t)) typeof(ModelDb).GetMethod("Inject",stat)!.Invoke(null,[t]);
Creature Make(CombatSide side) {
 var c=new Creature((Player)RuntimeHelpers.GetUninitializedObject(typeof(Player)),50,50);
 typeof(Creature).GetField("<Side>k__BackingField",inst)!.SetValue(c,side);return c;
}
var owner=Make(CombatSide.Player);var ally=Make(CombatSide.Player);var enemy=Make(CombatSide.Enemy);
var power=(RetreatingDefensePower)ModelDb.Power<RetreatingDefensePower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(power,owner);power.SetAmount(2,false);
var harmony=new HarmonyLib.Harmony("NightMustStay.RetreatingDefense.Tests");
var damageBody=typeof(RetreatingDefensePower).GetMethod(nameof(RetreatingDefensePower.AfterDamageReceived))!
 .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
harmony.Patch(AccessTools.Method(damageBody,"MoveNext"),transpiler:new HarmonyMethod(typeof(Fixture),nameof(Fixture.CaptureApply)));
var remove=typeof(PowerCmd).GetMethods(stat).Single(m=>m.Name=="Remove" && !m.IsGenericMethod && m.GetParameters().Length==1);
harmony.Patch(remove,prefix:new HarmonyMethod(typeof(Fixture).GetMethod(nameof(Fixture.Remove))!));
async Task Hit(Creature target,Creature? dealer,ValueProp props,int blocked,int unblocked) {
 var result=new DamageResult(target,props){BlockedDamage=blocked,UnblockedDamage=unblocked};
 await power.AfterDamageReceived(new BlockingPlayerChoiceContext(),target,result,props,dealer!,null!);
}
try {
 await Hit(owner,enemy,ValueProp.Move,4,0);
 Check(Fixture.Total==4,$"First full block failed: total={Fixture.Total}, ownerMatch={power.Owner==owner}, dealerSide={enemy.Side}/{(int)enemy.Side}, powered={ValueProp.Move.IsPoweredAttack()}.");
 await Hit(owner,enemy,ValueProp.Move,3,5);
 await Hit(owner,enemy,ValueProp.Move,2,0);
 Check(Fixture.Total==9 && Fixture.Calls==3,"Partial block or multi-hit conversion incorrect; repeated plays multiplied conversion.");
 await Hit(ally,enemy,ValueProp.Move,8,0);
 await Hit(owner,ally,ValueProp.Move,8,0);
 await Hit(owner,null,ValueProp.Move,8,0);
 await Hit(owner,enemy,ValueProp.Unpowered,8,0);
 await Hit(owner,enemy,ValueProp.Move,0,8);
 Check(Fixture.Total==9 && ReferenceEquals(Fixture.Target,owner),"Converted ally/non-attack/zero damage.");
 await power.AfterSideTurnStart(CombatSide.Enemy,[enemy],null!);
 Check(Fixture.Removes==0,"Expired before enemy attacks.");
 await power.AfterSideTurnStart(CombatSide.Player,[owner,ally],null!);
 Check(Fixture.Removes==1,"Did not expire at next owner turn, including repeated plays.");
 var card=(RetreatingDefense)ModelDb.Card<RetreatingDefense>().MutableClone();
 Check(card.DynamicVars.Block.BaseValue==4 && card.Keywords.Contains(CardKeyword.Exhaust),"Base card changed.");
 typeof(CardModel).GetMethod("UpgradeInternal",inst)!.Invoke(card,null);
 Check(card.DynamicVars.Block.BaseValue==4 && !card.Keywords.Contains(CardKeyword.Exhaust),"Upgrade incorrect.");
} finally {harmony.UnpatchAll(harmony.Id);}
Console.WriteLine("PASS: immediate full/partial/multi-hit conversion, no stack multiplier, ally and non-attack filters, next-owner-turn expiration, base/upgrade.");
public static class Fixture {
 public static decimal Total;public static int Calls,Removes;public static Creature? Target;
 public static IEnumerable<CodeInstruction> CaptureApply(IEnumerable<CodeInstruction> instructions) {
  int replaced=0;
  foreach(var instruction in instructions) {
   if(instruction.operand is MethodInfo method && method.DeclaringType==typeof(PowerCmd)
    && method.Name=="Apply" && method.IsGenericMethod && method.GetGenericArguments()[0]==typeof(GuardCounterPower)) {
    instruction.operand=AccessTools.Method(typeof(Fixture),nameof(Apply));replaced++;
   }
   yield return instruction;
  }
  if(replaced!=1) throw new Exception("Expected exactly one Guard Counter application.");
 }
 public static Task<GuardCounterPower> Apply(PlayerChoiceContext context,Creature target,decimal amount,Creature applier,CardModel card,bool silent) {
  Target=target;Total+=amount;Calls++;return Task.FromResult<GuardCounterPower>(null!);
 }
 public static bool Remove(ref Task __result) {Removes++;__result=Task.CompletedTask;return false;}
}

