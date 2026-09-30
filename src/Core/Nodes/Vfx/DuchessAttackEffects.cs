#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

public static class DuchessAttackEffects
{
    public static DuchessAttackVfx.Kind? KindFor(CardModel card) => card.GetType().Name switch
    {
        "DuchessRadiantBlade" => DuchessAttackVfx.Kind.Glintblade,
        "DuchessCarianSlicer" => DuchessAttackVfx.Kind.Slicer,
        "DuchessGreatCaria" => DuchessAttackVfx.Kind.GreatCaria,
        "DuchessCarianGreatsword" => DuchessAttackVfx.Kind.Greatsword,
        "DuchessCarianPiercer" => DuchessAttackVfx.Kind.Piercer,
        "DuchessRestage" or "DuchessReenactment" or "DuchessFleetingInstant" => DuchessAttackVfx.Kind.Clock,
        "DuchessLorettaGreatbow" => DuchessAttackVfx.Kind.Greatbow,
        "DuchessLorettaMastery" => DuchessAttackVfx.Kind.Mastery,
        "DuchessDeathBlade" => DuchessAttackVfx.Kind.DeathBlade,
        "DuchessGoldenBlade" => DuchessAttackVfx.Kind.GoldenBlade,
        "DuchessMiquellasHalo" => DuchessAttackVfx.Kind.Miquella,
        "DuchessSacredHalo" => DuchessAttackVfx.Kind.Sacred,
        _ => null
    };

    public static Node2D? Create(CardModel card, Creature target) =>
        KindFor(card) is { } kind ? Create(target, kind, card.Owner?.Creature) : DuchessSlashVfx.Create(target);

    public static Node2D? Create(Creature target, DuchessAttackVfx.Kind kind, Creature? attacker = null)
    {
        if (TestMode.IsOn || target.GetCreatureNode() is not { } node) return null;
        return new DuchessAttackVfx {
            SoundEnabled = true,
            AttackKind = kind, GlobalPosition = node.VfxSpawnPosition, ZIndex = 30,
            Source = attacker?.GetCreatureNode() is { } source
                ? source.VfxSpawnPosition-node.VfxSpawnPosition : new Vector2(-300,0)
        };
    }
}
