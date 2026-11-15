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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Compatibility;

/// <summary>
/// Runtime bridge for API signatures that differ between the v0.111 main
/// branch and the v0.113 Public Beta. Calls are resolved against the currently
/// loaded game assembly so one NightMustStay DLL can run on both branches.
/// </summary>
public static class Sts2BranchCompat
{
    private static readonly MethodInfo[] DamageMethods =
        typeof(CreatureCmd).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(CreatureCmd.Damage))
            .ToArray();

    private static readonly MethodInfo AttackFromCardMethod =
        typeof(AttackCommand).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method => method.Name == nameof(AttackCommand.FromCard));

    private static readonly MethodInfo ExhaustMethod =
        typeof(CardCmd).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(CardCmd.Exhaust)
                && method.GetParameters().Length >= 2
                && method.GetParameters()[0].ParameterType == typeof(PlayerChoiceContext)
                && method.GetParameters()[1].ParameterType == typeof(CardModel))
            .OrderBy(method => method.GetParameters().Length)
            .First();

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

    public static void SetCreatureInteractable(NCreature creature, bool on)
    {
        MethodInfo? method = typeof(NCreature).GetMethod(
            "SetIsInteractable",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(bool), typeof(bool) },
            modifiers: null);
        if (method is not null)
        {
            method.Invoke(creature, new object[] { on, true });
            return;
        }

        method = typeof(NCreature).GetMethod(
            "ToggleIsInteractable",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(bool) },
            modifiers: null)
            ?? throw new MissingMethodException(
                typeof(NCreature).FullName,
                "SetIsInteractable(bool, bool) / ToggleIsInteractable(bool)");
        method.Invoke(creature, new object[] { on });
    }

    public static Task Exhaust(PlayerChoiceContext context, CardModel card)
    {
        ParameterInfo[] parameters = ExhaustMethod.GetParameters();
        object?[] arguments = parameters.Select((parameter, index) => index switch
        {
            0 => context,
            1 => card,
            _ => parameter.DefaultValue,
        }).ToArray();
        return (Task)ExhaustMethod.Invoke(null, arguments)!;
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
        (MethodInfo method, object?[] invocationArguments) = PrepareDamageInvocation(arguments);
        return (Task<IEnumerable<DamageResult>>)method.Invoke(null, invocationArguments)!;
    }

    private static (MethodInfo Method, object?[] Arguments) PrepareDamageInvocation(object?[] suppliedArguments)
    {
        MethodInfo? exact = TryFindDamage(suppliedArguments);
        if (exact is not null)
            return (exact, suppliedArguments);

        foreach (MethodInfo method in DamageMethods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length <= suppliedArguments.Length
                || !ParametersAccept(parameters.Take(suppliedArguments.Length).ToArray(), suppliedArguments))
                continue;

            object?[] invocationArguments = new object?[parameters.Length];
            Array.Copy(suppliedArguments, invocationArguments, suppliedArguments.Length);
            bool supported = true;
            for (int i = suppliedArguments.Length; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(CardPlay))
                    invocationArguments[i] = null;
                else if (parameters[i].ParameterType == typeof(ModifyDamageHookType))
                    invocationArguments[i] = ModifyDamageHookType.All;
                else if (parameters[i].HasDefaultValue)
                    invocationArguments[i] = parameters[i].DefaultValue;
                else
                {
                    supported = false;
                    break;
                }
            }

            if (supported)
                return (method, invocationArguments);
        }

        throw MissingDamageMethod(suppliedArguments);
    }

    private static MethodInfo FindDamage(object?[] arguments) =>
        TryFindDamage(arguments)
        ?? throw MissingDamageMethod(arguments);

    private static MissingMethodException MissingDamageMethod(object?[] arguments) =>
        new(
            typeof(CreatureCmd).FullName,
            $"Damage({string.Join(", ", arguments.Select(argument => argument?.GetType().Name ?? "null"))})");

    private static MethodInfo? TryFindDamage(object?[] arguments) =>
        DamageMethods.FirstOrDefault(method => ParametersAccept(method.GetParameters(), arguments));

    private static bool ParametersAccept(ParameterInfo[] parameters, object?[] arguments)
    {
        if (parameters.Length != arguments.Length)
            return false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (arguments[i] is null)
            {
                Type parameterType = parameters[i].ParameterType;
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                    return false;
            }
            else if (!parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                return false;
        }
        return true;
    }
}
