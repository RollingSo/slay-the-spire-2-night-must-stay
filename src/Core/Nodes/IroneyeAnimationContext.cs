#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace NightMustStay.Core.Nodes;

/// <summary>Presentation metadata scoped to an executing command's async flow.</summary>
internal static class IroneyeAnimationContext
{
    internal enum AttackKind { Shot, Knife }
    private sealed class Registration { internal AttackKind Kind; }
    internal sealed record Scope(Creature? Actor, AttackKind Kind, Scope? Parent);
    private static readonly ConditionalWeakTable<AttackCommand, Registration> Commands = new();
    private static readonly AsyncLocal<Scope?> Current = new();

    internal static AttackCommand Register(AttackCommand command, AttackKind kind)
    {
        Commands.GetValue(command, _ => new Registration()).Kind = kind;
        return command;
    }

    internal static Scope? Enter(AttackCommand command)
    {
        Scope? previous = Current.Value;
        AttackKind kind = Commands.TryGetValue(command, out Registration? value)
            ? value.Kind : AttackKind.Shot;
        Current.Value = new Scope(command.Attacker, kind, previous);
        return previous;
    }

    internal static void Restore(Scope? previous) => Current.Value = previous;

    internal static AttackKind For(Creature actor)
    {
        for (Scope? scope = Current.Value; scope != null; scope = scope.Parent)
            if (ReferenceEquals(scope.Actor, actor)) return scope.Kind;
        return AttackKind.Shot;
    }
}
