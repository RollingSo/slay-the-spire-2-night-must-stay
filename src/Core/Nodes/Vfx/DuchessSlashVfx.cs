using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

public partial class DuchessSlashVfx : Node2D
{
    private float _age;
    private bool _echo;
    public static Node2D Create(Creature target)
    {
        if (TestMode.IsOn || target?.GetCreatureNode() is not { } node) return null;
        return new DuchessSlashVfx { GlobalPosition = node.VfxSpawnPosition, ZIndex = 25 };
    }
    public static void PlayEcho(Creature target)
    {
        if (Create(target) is not DuchessSlashVfx effect) return;
        effect._echo = true;
        target.GetCreatureNode().GetTree().CurrentScene.AddChild(effect);
    }
    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= 0.45f) { QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float alpha = 1f - _age / 0.45f;
        float spread = 80f + _age * 100f;
        Color blue = new(0.42f, 0.78f, 1f, alpha);
        Color silver = new(0.9f, 0.96f, 1f, alpha);
        DrawColoredPolygon(new[] { new Vector2(-spread, 65), new Vector2(spread, -70), new Vector2(10, -2) }, silver);
        DrawLine(new Vector2(-spread, 73), new Vector2(spread, -62), blue, 5f);
        if (_echo)
        {
            DrawColoredPolygon(new[] { new Vector2(-spread, 90), new Vector2(spread, -45), new Vector2(0, 26) }, blue);
            DrawArc(Vector2.Zero, 25f + _age * 125f, -1.2f, 3.5f, 20, blue, 4f);
        }
    }
}
