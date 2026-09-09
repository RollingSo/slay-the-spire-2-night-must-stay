using NightMustStay.Core.Nodes.Vfx;

public partial class GuardianSample : GuardianAttackVfx
{
    public float T;
    public override void _Process(double delta) { }
    public override void _Draw() { }
}
public partial class IroneyeSample : IroneyeAttackVfx
{
    public float T;
    public override void _Process(double delta) { }
    public override void _Draw() { }
}
public partial class RevenantSample : RevenantAttackVfx
{
    public float T;
    public override void _Process(double delta) { }
    public override void _Draw() { }
}

public partial class MarkSample : IroneyeMarkGlyph
{
    public float T;
    public bool Idle;
    public override void _Process(double delta) { }
    public override void _Draw() => DrawMark(T,Idle?0:System.Math.Clamp(1-T*2.8f,0,1));
}
