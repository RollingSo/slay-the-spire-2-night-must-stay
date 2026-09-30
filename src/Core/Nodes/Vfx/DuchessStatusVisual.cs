using Godot;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Read-only status presentation; state removal and revival restore the sprite.</summary>
public partial class DuchessStatusVisual : Node
{
    public NCreature CreatureNode { get; set; }
    private float _opacity = 1;
    private float _age;
    private bool _wasGhost;
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(CreatureNode)) { QueueFree(); return; }
        var creature = CreatureNode.Entity;
        var rig = CreatureNode.Visuals?.GetNodeOrNull<Node>("Visuals/Prototype");
        if (creature == null || rig == null) return;
        bool ghost = creature.HasPower<DuchessConcealmentPower>() || creature.HasPower<IntangiblePower>();
        if (ghost != _wasGhost)
        {
            _wasGhost = ghost;
            DuchessAudio.Play(ghost ? "dark_orb_channel.mp3" : "frost_orb_passive.mp3", .3f);
        }
        _age += (float)delta;
        _opacity = Mathf.MoveToward(_opacity, ghost ? .32f : 1f, (float)delta*2.8f);
        if (rig.HasMethod("set_phase_visual"))
            rig.Call("set_phase_visual", _opacity, ghost ? .5f+.5f*Mathf.Sin(_age*3f) : 0f);
    }
}
