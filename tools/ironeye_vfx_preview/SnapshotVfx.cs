using NightMustStay.Core.Nodes.Vfx;

public partial class SnapshotVfx : IroneyeAttackVfx
{
    public float FrameTime { get; set; }
    public override void _Process(double delta) { }
    public override void _Draw() => DrawFrame(FrameTime);
}
