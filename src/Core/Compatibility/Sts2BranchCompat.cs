#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Compatibility;

/// <summary>
/// Runtime bridge for API signatures changed by the v0.108 Public Beta.
/// Calls are resolved once against the currently loaded game assembly so the
/// same NightMustStay DLL can run on both Production and Public Beta.
/// </summary>
public static class Sts2BranchCompat
{
    private static readonly MethodInfo AttackFromCardMethod =
        typeof(AttackCommand).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method => method.Name == nameof(AttackCommand.FromCard));

    public static AttackCommand AttackFromCard(AttackCommand command, CardModel card) =>
        (AttackCommand)AttackFromCardMethod.Invoke(
            command,
            AttackFromCardMethod.GetParameters().Length == 1
                ? new object?[] { card }
                : new object?[] { card, null })!;

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        Creature target,
        DamageVar damage,
        CardModel cardSource) =>
        InvokeDamageWithOptionalCardPlay(context, target, damage, cardSource);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        Creature target,
        decimal amount,
        ValueProp props,
        CardModel cardSource) =>
        InvokeDamageWithOptionalCardPlay(context, target, amount, props, cardSource);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        IEnumerable<Creature> targets,
        DamageVar damage,
        Creature dealer) =>
        InvokeDamage(context, targets, damage, dealer);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature dealer) =>
        InvokeDamage(context, targets, amount, props, dealer);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        Creature target,
        DamageVar damage,
        Creature dealer) =>
        InvokeDamage(context, target, damage, dealer);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer) =>
        InvokeDamage(context, target, amount, props, dealer);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        Creature target,
        DamageVar damage,
        Creature dealer,
        CardModel cardSource) =>
        InvokeDamageWithOptionalCardPlay(context, target, damage, dealer, cardSource);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource) =>
        InvokeDamageWithOptionalCardPlay(context, target, amount, props, dealer, cardSource);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        IEnumerable<Creature> targets,
        DamageVar damage,
        Creature dealer,
        CardModel cardSource) =>
        InvokeDamageWithOptionalCardPlay(context, targets, damage, dealer, cardSource);

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext context,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource) =>
        InvokeDamageWithOptionalCardPlay(context, targets, amount, props, dealer, cardSource);

    public static Task LoseBlock(Creature creature, decimal amount)
    {
        MethodInfo method = typeof(CreatureCmd).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == nameof(CreatureCmd.LoseBlock));
        object?[] arguments = method.GetParameters().Length == 2
            ? new object?[] { creature, amount }
            : new object?[] { new BlockingPlayerChoiceContext(), creature, amount, null };
        return (Task)method.Invoke(null, arguments)!;
    }

    public static decimal ModifyDamage(
        IRunState runState,
        ICombatState? combatState,
        Creature? target,
        Creature? dealer,
        decimal amount,
        ValueProp props,
        CardModel? cardSource,
        ModifyDamageHookType hookType,
        CardPreviewMode previewMode,
        out IEnumerable<AbstractModel> modifiers)
    {
        MethodInfo method = typeof(Hook).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == nameof(Hook.ModifyDamage));
        object?[] arguments = method.GetParameters().Length == 10
            ? new object?[]
            {
                runState, combatState, target, dealer, amount, props, cardSource,
                hookType, previewMode, null,
            }
            : new object?[]
            {
                runState, combatState, target, dealer, amount, props, cardSource,
                null, hookType, previewMode, null,
            };
        decimal result = (decimal)method.Invoke(null, arguments)!;
        modifiers = (IEnumerable<AbstractModel>)arguments[^1]!;
        return result;
    }

    public static void RegisterSavedPropertyType(Type modelType)
    {
        Type? cacheType = typeof(CardModel).Assembly.GetType(
            "MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache");
        cacheType?.GetMethod(
                "InjectTypeIntoCache",
                BindingFlags.Public | BindingFlags.Static)
            ?.Invoke(null, new object[] { modelType });
    }

    private static Task<IEnumerable<DamageResult>> InvokeDamage(params object?[] arguments)
    {
        MethodInfo method = FindDamage(arguments);
        return (Task<IEnumerable<DamageResult>>)method.Invoke(null, arguments)!;
    }

    private static Task<IEnumerable<DamageResult>> InvokeDamageWithOptionalCardPlay(params object?[] arguments)
    {
        MethodInfo? method = TryFindDamage(arguments);
        if (method is null)
        {
            arguments = arguments.Append(null).ToArray();
            method = FindDamage(arguments);
        }
        return (Task<IEnumerable<DamageResult>>)method.Invoke(null, arguments)!;
    }

    private static MethodInfo FindDamage(object?[] arguments) =>
        TryFindDamage(arguments)
        ?? throw new MissingMethodException(
            typeof(CreatureCmd).FullName,
            $"Damage({string.Join(", ", arguments.Select(argument => argument?.GetType().Name ?? "null"))})");

    private static MethodInfo? TryFindDamage(object?[] arguments) =>
        typeof(CreatureCmd).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(CreatureCmd.Damage))
            .FirstOrDefault(method => ParametersAccept(method.GetParameters(), arguments));

    private static bool ParametersAccept(ParameterInfo[] parameters, object?[] arguments)
    {
        if (parameters.Length != arguments.Length)
            return false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (arguments[i] is not null && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                return false;
        }
        return true;
    }
}
