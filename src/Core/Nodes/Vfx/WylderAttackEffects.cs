#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

public static class WylderAttackEffects
{
    public static AttackCommand WithWylderFx(this AttackCommand command, WylderAttackVfx.Kind kind, Creature attacker) =>
        command.WithHitFx().WithHitVfxNode(target => Create(target, kind, attacker));

    public static AttackCommand WithEndTheNightFx(this AttackCommand command) =>
        command.WithHitFx().WithHitVfxNode(WylderEndTheNightVfx.CreateImpact);

    public static Node2D? Create(Creature target, WylderAttackVfx.Kind kind, Creature attacker)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return null;
        var targetNode = target.GetCreatureNode();
        if (targetNode == null) return null;
        Vector2 origin = new(-260f, 0f);
        if (attacker?.GetCreatureNode() is { } source)
            origin = source.VfxSpawnPosition - targetNode.VfxSpawnPosition;
        return new WylderAttackVfx
        {
            Name = $"Wylder{kind}Vfx",
            AttackKind = kind,
            Origin = origin,
            GlobalPosition = targetNode.VfxSpawnPosition,
            ZIndex = 24,
        };
    }
}
