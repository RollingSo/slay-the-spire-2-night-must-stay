using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

try
{
    VerifyRandomHitsUseAttackCommand();
    VerifyFreezeDamageFiltering();
    VerifyChargeRightClickGuard();
    VerifyCardDamageUsesDynamicVars();
    VerifySpaceRendingFrenzyTargeting();
    VerifyWhiteShadowLureProtection();
    Console.WriteLine(
        "PASS: Revenant attacks, dynamic damage values, Freeze filtering, charge-card right-click cancellation, Space-Rending Frenzy targeting, and White Shadow Lure protection are regression-covered.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
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

static void VerifyCardDamageUsesDynamicVars()
{
    var lansseaxBlade = new LansseaxBlade();
    if (lansseaxBlade.DynamicVars.Damage.BaseValue != 63m)
        throw new InvalidOperationException("Lansseax Blade must expose its 63 damage through DamageVar.");

    var formationBreakerHammer = new FormationBreakerHammer();
    DynamicVar frederickDamage = formationBreakerHammer.DynamicVars["FamilyDamage"];
    if (frederickDamage.BaseValue != 20m
        || frederickDamage is not DamageVar { Props: ValueProp.Move }
        || frederickDamage.GetType().Name != "RevenantFamilyDamageVar")
        throw new InvalidOperationException("Formation Breaker Hammer must expose Frederick's damage dynamically.");

    var giantSkeletonWrath = new GiantSkeletonWrath();
    DynamicVar sebastianDamage = giantSkeletonWrath.DynamicVars["FamilyDamage"];
    if (sebastianDamage.BaseValue != 4m
        || sebastianDamage is not DamageVar { Props: ValueProp.Move }
        || sebastianDamage.GetType().Name != "RevenantFamilyDamageVar"
        || giantSkeletonWrath.DynamicVars.Repeat.IntValue != 3)
    {
        throw new InvalidOperationException(
            "Giant Skeleton Wrath must expose Sebastian's damage and hit count dynamically.");
    }
}

static void VerifySpaceRendingFrenzyTargeting()
{
    var card = new SpaceRendingFrenzy();
    if (card.TargetType != MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy
        || card.DynamicVars.Damage.BaseValue != 16m
        || card.DynamicVars["FamilyDamage"].BaseValue != 5m)
        throw new InvalidOperationException("Space-Rending Frenzy must keep its selected-enemy target and 16 damage / 5 family HP cost.");

    MethodInfo onPlay = typeof(SpaceRendingFrenzy).GetMethod(
        "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)!;
    Type stateMachine = onPlay.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
    MethodInfo moveNext = stateMachine.GetMethod(
        "MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    MethodBase[] calls = ReadCalledMethods(moveNext).ToArray();
    if (!calls.Any(call => call.Name == "get_Target")
        || !calls.Any(call => call.Name == "DamageFamily")
        || !calls.Any(call => call.Name == "Damage"
            && call.DeclaringType?.Name == "RevenantAttackEffects")
        || calls.Any(call => call.Name is "NextItem" or "get_CombatTargets" or "get_HittableEnemies"))
        throw new InvalidOperationException("Space-Rending Frenzy must use CardPlay.Target, not select a random enemy.");

    typeof(AbstractModel).GetMethod("NeverEverCallThisOutsideOfTests_SetIsMutable",
        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(card, new object[] { true });
    typeof(SpaceRendingFrenzy).GetMethod("OnUpgrade",
        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(card, null);
    if (card.DynamicVars.Damage.BaseValue != 20m
        || card.DynamicVars["FamilyDamage"].BaseValue != 5m)
        throw new InvalidOperationException("Upgraded Space-Rending Frenzy must keep 20 damage / 5 family HP cost.");
}

static void VerifyWhiteShadowLureProtection()
{
    var card = new WhiteShadowLure();
    if (card.EnergyCost.BaseValue != 0m)
        throw new InvalidOperationException("White Shadow Lure must cost 0 Energy.");
    if (!card.CanonicalKeywords.Contains(MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Exhaust))
        throw new InvalidOperationException("White Shadow Lure must Exhaust after use.");

    MethodInfo routeDamage = typeof(RevenantSummonControllerPower).GetMethod(
        "AfterModifyingHpLostBeforeOsty",
        BindingFlags.Instance | BindingFlags.Public)!;
    Type stateMachine = routeDamage.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
        ?? throw new InvalidOperationException("Revenant damage routing is no longer an async state machine.");
    MethodInfo moveNext = stateMachine.GetMethod(
        "MoveNext",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    MethodBase[] calls = ReadCalledMethods(moveNext).ToArray();
    if (!calls.Any(call => call.Name == "HasPower"
            && call.IsGenericMethod
            && call.GetGenericArguments().SingleOrDefault() == typeof(MegaCrit.Sts2.Core.Models.Powers.BufferPower)))
    {
        throw new InvalidOperationException(
            "Revenant damage routing must recognize Buffer before allowing damage to overflow to the Revenant.");
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
