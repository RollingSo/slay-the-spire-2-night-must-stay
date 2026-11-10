using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Read-only presentation, including saved fights and recreated visuals.</summary>
public partial class WylderStatusVisual : Node
{
    public NCreature CreatureNode { get; set; }
    private Node _lastRig;
    private bool? _lastActive;

    public override void _Ready() => _Process(0);

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(CreatureNode)) { QueueFree(); return; }
        var rig = CreatureNode.Visuals?.GetNodeOrNull<Node>("Visuals/Prototype");
        if (CreatureNode.Entity == null || rig == null || !rig.HasMethod("set_pursuit_active")) return;
        bool active = WylderPursuitPower.IsActive(CreatureNode.Entity);
        if (rig == _lastRig && active == _lastActive) return;
        rig.Call("set_pursuit_active", active);
        _lastRig = rig;
        _lastActive = active;
    }
}
