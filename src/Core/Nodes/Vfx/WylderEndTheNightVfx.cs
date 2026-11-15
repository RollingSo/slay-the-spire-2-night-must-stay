#nullable enable
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>
/// Reuses the production Grand Finale cadence, with a bounded Wylder overlay:
/// one screen tint, three arcs, twelve rays, then self-destruction.
/// </summary>
public partial class WylderEndTheNightVfx : Node2D
{
    private enum Phase { Prelude, Impact }
    private Phase _phase;
    private float _age;
    private Vector2 _focus;

    public static async Task PlayPrelude(Creature owner)
    {
        if (TestMode.IsOn) return;
        Control? container = NCombatRoom.Instance?.CombatVfxContainer;
        var original = NGrandFinaleVfx.Create(owner);
        if (container != null && original != null) container.AddChildSafely(original);
        if (container != null && owner?.GetCreatureNode() is { } node)
            container.AddChildSafely(new WylderEndTheNightVfx { _phase = Phase.Prelude, _focus = node.VfxSpawnPosition, ZIndex = 28 });
        await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration, false);
    }

    public static Node2D? CreateImpact(Creature target)
    {
        if (TestMode.IsOn || target?.GetCreatureNode() is not { } node) return null;
        var root = new WylderEndTheNightVfx { _phase = Phase.Impact, _focus = node.VfxSpawnPosition, ZIndex = 29 };
        var original = NGrandFinaleImpactVfx.Create(target);
        if (original != null) root.AddChild(original);
        return root;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float lifetime = _phase == Phase.Prelude ? NGrandFinaleVfx.totalAnticipationDuration + .08f : .76f;
        if (_age >= lifetime) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_phase == Phase.Prelude) DrawPrelude(); else DrawImpact();
    }

    private void DrawPrelude()
    {
        float t = Mathf.Clamp(_age / NGrandFinaleVfx.totalAnticipationDuration, 0f, 1f);
        Vector2 size = GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero, size), new Color("13060A", .12f + .28f * t));
        for (int i = 0; i < 3; i++)
            DrawArc(_focus, Mathf.Lerp(210f - i * 26f, 74f - i * 8f, t), -2.7f + i * .3f, 1.1f + i * .3f, 48,
                new Color(i == 1 ? "FFD36A" : "C9273C", .25f + .65f * t), 8f - i * 2f, true);
    }

    private void DrawImpact()
    {
        float t = Mathf.Clamp(_age / .76f, 0f, 1f);
        float fade = 1f - t;
        float r = Mathf.Lerp(30f, 245f, Mathf.Sqrt(t));
        DrawCircle(_focus, r * .28f, new Color("FFF1BD", .34f * fade));
        for (int i = 0; i < 12; i++)
        {
            Vector2 d = Vector2.FromAngle(i * Mathf.Tau / 12f + .13f);
            DrawLine(_focus + d * r * .22f, _focus + d * r, new Color(i % 3 == 0 ? "FFD36A" : "C9273C", fade), i % 3 == 0 ? 10f : 6f, true);
        }
    }
}
