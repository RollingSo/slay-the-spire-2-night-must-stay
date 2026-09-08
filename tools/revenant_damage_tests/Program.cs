using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

try
{
    VerifyRandomHitsUseAttackCommand();
    VerifyFreezeDamageFiltering();
    VerifyChargeRightClickGuard();
    VerifyUndyingMarchLifetime();
    VerifyFamilyCallStats();
    VerifySpiritFormStats();
    VerifyFamilyIntentDamage();
    Console.WriteLine(
        "PASS: Revenant attacks, Freeze filtering, charge-card cancellation, Undying March lifetime, Family Call HP, Spirit Form HP, and Family intents are regression-covered.");
    return 0;
}

catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}

static void VerifyFamilyIntentDamage()
{
    var helen = new RevenantFamilyAttackIntent(3);
    var frederick = new RevenantFamilyAttackIntent(5, 2);
    if (helen.GetTotalDamage(Array.Empty<Creature>(), null!) != 3)
        throw new InvalidOperationException("Helen's family intent must show 3 damage.");
    if (frederick.GetTotalDamage(Array.Empty<Creature>(), null!) != 10)
        throw new InvalidOperationException("Multi-hit family intent total damage is incorrect.");

    MethodInfo refresh = typeof(RevenantSummonManager).GetMethod(
        "RefreshFamilyIntents",
        BindingFlags.Instance | BindingFlags.NonPublic)!;
    if (!ReadCalledMethods(refresh).Any(call =>
            call.DeclaringType == typeof(RevenantFamilyAttackIntent)
            && call.IsConstructor))
    {
        throw new InvalidOperationException(
            "Family intent rendering no longer uses the Vulnerable-independent intent type.");
    }
}

static void VerifyFamilyCallStats()
{
    Type manager = typeof(RevenantSummonManager);
    MethodInfo initialHp = manager.GetMethod(
        "GetInitialFamilyHp",
        BindingFlags.Static | BindingFlags.NonPublic)!;
    foreach (RevenantFamilyId family in Enum.GetValues<RevenantFamilyId>())
    {
        int hp = (int)initialHp.Invoke(null, new object[] { family })!;
        if (hp != 6)
            throw new InvalidOperationException($"{family} should start with 6 HP, but starts with {hp}.");
    }

    MethodInfo stackedHp = manager.GetMethod(
        "CalculateFamilyHpIncrease",
        BindingFlags.Static | BindingFlags.NonPublic)!;
    object result = stackedHp.Invoke(null, new object[] { 12, 4, 6 })!;
    int maxHp = (int)result.GetType().GetField("Item1")!.GetValue(result)!;
    int currentHp = (int)result.GetType().GetField("Item2")!.GetValue(result)!;
    if (maxHp != 18 || currentHp != 10)
    {
        throw new InvalidOperationException(
            $"Calling with a Family present should add 6 Max HP and 6 current HP; got {maxHp}/{currentHp}.");
    }
}

static void VerifySpiritFormStats()
{
    var power = new SpiritFormPower();
    if (SpiritFormPower.FamilyHpGain != 6)
        throw new InvalidOperationException("Spirit Form should grant 6 Family HP.");
    if (power.StackType.ToString() != "Single")
        throw new InvalidOperationException("Spirit Form should trigger once per turn rather than once per stack.");

    MethodInfo trigger = typeof(SpiritFormPower).GetMethod(nameof(SpiritFormPower.AfterPlayerTurnStartLate))!;
    Type stateMachine = trigger.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
        ?? throw new InvalidOperationException("Spirit Form trigger is no longer asynchronous.");
    MethodInfo moveNext = stateMachine.GetMethod(
        "MoveNext",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    MethodBase[] calls = ReadCalledMethods(moveNext).ToArray();
    if (!calls.Any(call => call.Name == nameof(RevenantSummonManager.IncreaseFamilyMaxAndCurrentHp))
        || !calls.Any(call => call.Name == nameof(RevenantSummonManager.TriggerResonance)))
    {
        throw new InvalidOperationException(
            "Spirit Form must increase both Family Max/current HP and then trigger Resonance.");
    }
}

static void VerifyRandomHitsUseAttackCommand()
{
    Type helpers = typeof(GurranqsRock).Assembly.GetType(
        "NightMustStay.Core.Models.Cards.RevenantCardHelpers",
        throwOnError: true)!;
    MethodInfo helper = helpers.GetMethod(
        "DamageRandomEachHit",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
    Type stateMachine = helper.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
        ?? throw new InvalidOperationException("DamageRandomEachHit is no longer an async state machine.");
    MethodInfo moveNext = stateMachine.GetMethod(
        "MoveNext",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    MethodBase[] calls = ReadCalledMethods(moveNext).ToArray();
    if (!calls.Any(call => call.DeclaringType == typeof(DamageCmd) && call.Name == "Attack"))
        throw new InvalidOperationException("DamageRandomEachHit no longer constructs an AttackCommand.");
    if (!calls.Any(call => call.Name == "WithHitCount")
        || !calls.Any(call => call.Name == "TargetingRandomOpponents"))
    {
        throw new InvalidOperationException(
            "DamageRandomEachHit no longer executes one randomly-targeted multi-hit attack.");
    }
}

static void VerifyFreezeDamageFiltering()
{
    typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
    Type patch = typeof(GurranqsRock).Assembly.GetType(
        "NightMustStay.Core.Patches.FreezeDamageBranchPatch",
        throwOnError: true)!;
    MethodInfo beforeModify = patch.GetMethod(
        "BeforeModify",
        BindingFlags.Static | BindingFlags.NonPublic)!;

    var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
    var owner = new Creature(player, 50, 50);
    var freeze = new FreezePower();
    typeof(AbstractModel).GetMethod(
        "NeverEverCallThisOutsideOfTests_SetIsMutable",
        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(freeze, new object[] { true });
    typeof(PowerModel).GetProperty("Owner")!.SetValue(freeze, owner);
    freeze.SetAmount(3, false);

    decimal attackBonus = InvokeFreezeModifier(beforeModify, freeze, owner, ValueProp.Move);
    decimal thornsBonus = InvokeFreezeModifier(beforeModify, freeze, owner, ValueProp.Unpowered);
    if (attackBonus != 3m)
        throw new InvalidOperationException($"Freeze should add 3 to attack damage, but added {attackBonus}.");
    if (thornsBonus != 0m)
        throw new InvalidOperationException($"Freeze should not add to Unpowered/Thorns damage, but added {thornsBonus}.");

    var harmony = new HarmonyLib.Harmony("NightMustStay.RevenantDamage.Tests");
    harmony.CreateClassProcessor(patch).Patch();
    harmony.UnpatchAll(harmony.Id);
}

static void VerifyChargeRightClickGuard()
{
    Type patch = typeof(GurranqsRock).Assembly.GetType(
        "NightMustStay.Core.Patches.RevenantDirectPlayFallbackPatch",
        throwOnError: true)!;
    MethodInfo decision = patch.GetMethod(
        "ShouldCancelChargeDirectPlay",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
    bool ShouldCancel(CardModel card, bool mouse, bool right) =>
        (bool)decision.Invoke(null, new object[] { card, mouse, right })!;

    if (!ShouldCancel(new BeastClaw(), true, true)
        || ShouldCancel(new BeastClaw(), true, false)
        || ShouldCancel(new BeastClaw(), false, true)
        || ShouldCancel(new StrikeRevenant(), true, true))
    {
        throw new InvalidOperationException("Charge right-click cancellation decision is incorrect.");
    }

    MethodInfo prefix = patch.GetMethod(
        "BeforeTryPlayCard",
        BindingFlags.Static | BindingFlags.NonPublic)!;
    MethodBase[] calls = ReadCalledMethods(prefix).ToArray();
    if (!calls.Any(call => call.DeclaringType == typeof(Input)
            && call.Name == nameof(Input.IsMouseButtonPressed))
        || !calls.Any(call => call.DeclaringType == typeof(NCardPlay)
            && call.Name == nameof(NCardPlay.CancelPlayCard)))
    {
        throw new InvalidOperationException(
            "TryPlayCard is no longer guarded by the right mouse button before synchronized play.");
    }

    if (calls.Any(call => call.Name is "get_Rng" or "NextItem"))
        throw new InvalidOperationException("Local charge-card input handling must not advance synchronized RNG.");

    var harmony = new HarmonyLib.Harmony("NightMustStay.ChargeInput.Tests");
    harmony.CreateClassProcessor(patch).Patch();
    harmony.UnpatchAll(harmony.Id);
}

static void VerifyUndyingMarchLifetime()
{
    Type powerType = typeof(UndyingMarchPower);
    MethodInfo? turnStart = powerType.GetMethod(
        "AfterSideTurnStart",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
    MethodInfo? turnEnd = powerType.GetMethod(
        "AfterSideTurnEnd",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    if (turnStart is null)
        throw new InvalidOperationException("Undying March must expire at the next allied turn start.");
    if (turnEnd is not null)
        throw new InvalidOperationException("Undying March must not expire at the turn end when it is played.");

    Type stateMachine = turnStart.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
        ?? throw new InvalidOperationException("Undying March turn-start hook is no longer asynchronous.");
    MethodInfo moveNext = stateMachine.GetMethod(
        "MoveNext",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    if (!ReadCalledMethods(moveNext).Any(call =>
            call.DeclaringType == typeof(PowerCmd) && call.Name == nameof(PowerCmd.Remove)))
    {
        throw new InvalidOperationException("Undying March turn-start hook no longer removes the power.");
    }
}

static decimal InvokeFreezeModifier(
    MethodInfo method,
    FreezePower freeze,
    Creature target,
    ValueProp props)
{
    object?[] arguments = { freeze, target, 10m, props, 0m };
    bool skipOriginal = !(bool)method.Invoke(null, arguments)!;
    if (!skipOriginal)
        throw new InvalidOperationException("Freeze prefix unexpectedly allowed the original modifier to run.");
    return (decimal)arguments[4]!;
}

static IEnumerable<MethodBase> ReadCalledMethods(MethodInfo method)
{
    byte[] il = method.GetMethodBody()?.GetILAsByteArray()
        ?? throw new InvalidOperationException($"{method} has no IL body.");
    Module module = method.Module;
    int index = 0;
    while (index < il.Length)
    {
        OpCode opCode = ReadOpCode(il, ref index);
        int operandStart = index;
        int operandSize = GetOperandSize(opCode.OperandType, il, operandStart);
        if (opCode.OperandType == OperandType.InlineMethod)
        {
            int token = BitConverter.ToInt32(il, operandStart);
            MethodBase? called = module.ResolveMethod(
                token,
                method.DeclaringType?.GetGenericArguments(),
                method.GetGenericArguments());
            if (called is not null)
                yield return called;
        }
        index += operandSize;
    }
}

static OpCode ReadOpCode(byte[] il, ref int index)
{
    byte first = il[index++];
    short value = first == 0xFE
        ? (short)(0xFE00 | il[index++])
        : first;
    return IlOpCodes.ByValue[value];
}

static int GetOperandSize(OperandType type, byte[] il, int index) => type switch
{
    OperandType.InlineNone => 0,
    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
    OperandType.InlineVar => 2,
    OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI
        or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString
        or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
    OperandType.InlineI8 or OperandType.InlineR => 8,
    OperandType.InlineSwitch => 4 + BitConverter.ToInt32(il, index) * 4,
    _ => throw new NotSupportedException($"Unsupported IL operand type {type}.")
};

internal static class IlOpCodes
{
    internal static readonly IReadOnlyDictionary<short, OpCode> ByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);
}
