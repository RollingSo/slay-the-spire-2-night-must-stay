using System;
using Godot;
using NightMustStay.Core.Nodes.Vfx;

public partial class Preview : Node2D
{
    private float _time;
    private int _frame;
    private bool _capture;
    private bool _verify;
    private readonly string[] _names = { "01  HALBERD / SILVER", "02  WHIRLWIND / CYAN", "03  COUNTER / OCHRE" };

    public override void _Ready()
    {
        ParticleVfxMaterials.AssetRoot=ProjectSettings.GlobalizePath("res://../../images/vfx/particle_remake/");
        _capture = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--capture");
        _verify = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--verify");
        for (int col = 0; col < 3; col++)
        for (int row = 0; row < 3; row++)
            AddChild(new SnapshotVfx { AttackKind = (GuardianAttackVfx.Kind)col,
                FrameTime = new[] { 0.12f, 0.32f, 0.67f }[row], Position = new Vector2(200 + col * 400, 173 + row * 190), Scale = new Vector2(0.72f, 0.72f) });
        Spawn();
        if (_verify)
            for (int i = 0; i < 30; i++)
                AddChild(new GuardianAttackVfx { AttackKind = (GuardianAttackVfx.Kind)(i % 3), Position = new Vector2(-500, -500) });
    }

    private void Spawn()
    {
        for (int i = 0; i < 3; i++)
            AddChild(new GuardianAttackVfx { AttackKind = (GuardianAttackVfx.Kind)i, Position = new Vector2(200 + i * 400, 730), Scale = new Vector2(0.65f, 0.65f) });
    }

    public override async void _Process(double delta)
    {
        _time += (float)delta;
        if (!_verify && _time > 1.1f) { _time = 0; Spawn(); }
        if (_verify)
        {
            _frame++;
            foreach (Node child in GetChildren())
                if (child is SnapshotVfx snapshot)
                {
                    snapshot.FrameTime = Math.Clamp((_frame - 1) / 120f, 0, 1);
                    snapshot.QueueRedraw();
                }
            if (_frame == 150)
            {
                int liveEffects = 0;
                foreach (Node child in GetChildren())
                    if (child is GuardianAttackVfx && child is not SnapshotVfx) liveEffects++;
                GD.Print($"VFX_LIFECYCLE: remaining={liveEffects}; all three styles sampled at 121 lifetime positions.");
                GetTree().Quit(liveEffects == 0 ? 0 : 1);
            }
        }
        if (_capture && ++_frame == 8)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string path = ProjectSettings.GlobalizePath("res://../../design/特效预览/guardian_attack_vfx_contact.png");
            DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(path));
            Error result = GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"VFX_CONTACT_CAPTURE: {result} {path}");
            GetTree().Quit(result == Error.Ok ? 0 : 1);
        }
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(30, 36), "GUARDIAN / THREE ATTACK SIGNATURES", fontSize: 24, modulate: new Color("#DCECF1"));
        DrawString(font, new Vector2(30, 61), "Production geometry | rows: 12%, 32%, 67% lifetime | bottom: looping playback", fontSize: 15, modulate: new Color("#90A4B4"));
        for (int col = 0; col < 3; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                DrawRect(new Rect2(12 + col * 400, 90 + row * 190, 376, 178), new Color(row == 1 ? "#354754" : "#1C2A36"));
                // A muted target silhouette makes occlusion and center alignment visible.
                DrawCircle(new Vector2(200 + col * 400, 162 + row * 190), 31, new Color("#4B555C"));
                DrawRect(new Rect2(174 + col * 400, 185 + row * 190, 52, 28), new Color("#4B555C"));
            }
            DrawString(font, new Vector2(30 + col * 400, 84), _names[col], fontSize: 16, modulate: new Color("#DCECF1"));
        }
        DrawString(font, new Vector2(30, 677), "LIVE LOOP / no audio in this isolated visual test", fontSize: 15, modulate: new Color("#90A4B4"));
    }
}
