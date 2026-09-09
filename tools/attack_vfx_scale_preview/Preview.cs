using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using NightMustStay.Core.Nodes.Vfx;
using G = NightMustStay.Core.Nodes.Vfx.GuardianAttackVfx.Kind;
using I = NightMustStay.Core.Nodes.Vfx.IroneyeAttackVfx.Kind;
using R = NightMustStay.Core.Nodes.Vfx.RevenantAttackVfx.Kind;

/// <summary>Links production drawing. All contact cells use the same 0.65 canvas scale.</summary>
public partial class Preview : Node2D
{
    private readonly List<(string Name, Func<Node2D> Make)> _cases = new();
    private readonly List<(string Name, Image Image, int Width, int Height, int Pixels)> _captures = new();
    private readonly List<Node2D> _lifecycle = new();
    private readonly List<object> _audit = new();
    private readonly List<string> _failures = new();
    private string _output = "";
    private bool _running;
    private int _page;
    private float _time;

    public override void _Ready()
    {
        ParticleVfxMaterials.AssetRoot = ProjectSettings.GlobalizePath("res://../../images/vfx/particle_remake/");
        string? pack=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--pack="));
        if(pack!=null)
        {
            if(!ProjectSettings.LoadResourcePack(pack.Substring(7),false))throw new Exception("Cannot load exported PCK.");
            ParticleVfxMaterials.AssetRoot="res://images/vfx/particle_remake/";
            for(int tile=0;tile<16;tile++)if(ParticleVfxMaterials.Texture(tile).GetWidth()<512)throw new Exception("Invalid packaged atlas.");
            GD.Print("PASS: all four production atlases loaded from exported PCK.");
        }
        _output = ProjectSettings.GlobalizePath("res://../../design/特效预览/particle_remake_20260910");
        Directory.CreateDirectory(_output);
        foreach (G k in Enum.GetValues<G>()) _cases.Add(("GUARDIAN / " + k, () => new GuardianSample { AttackKind = k }));
        foreach (I k in Enum.GetValues<I>()) _cases.Add(("IRONEYE / " + k, () => new IroneyeSample { AttackKind = k }));
        foreach (R k in Enum.GetValues<R>()) _cases.Add(("REVENANT / " + k, () => new RevenantSample { AttackKind = k }));
        foreach (decimal damage in new[] { 8m, 30m, 80m, 200m })
            _cases.Add(($"COUNTER / {damage} DAMAGE", () => new GuardianSample { AttackKind = G.Counter, VisualDamage = damage }));
        foreach (decimal damage in new[] { 8m, 30m, 80m, 200m })
            _cases.Add(($"HALO / {damage} DAMAGE", () => new RevenantSample { AttackKind = R.HaloOut, VisualDamage = damage }));
        _cases.Add(("MARK / ACQUISITION",()=>new MarkSample()));
        _cases.Add(("MARK / PERSISTENT READOUT",()=>new MarkSample{Idle=true}));
        VerifyMath();
        if (OS.GetCmdlineUserArgs().Contains("--movie"))
            RunMovie();
        else if (OS.GetCmdlineUserArgs().Contains("--verify"))
            RunVerification();
        else ShowLivePage();
    }

    private static void VerifyMath()
    {
        foreach (decimal d in new[] { decimal.MinValue, -1m, 0m, 8m, 30m, 80m, 200m, decimal.MaxValue })
        {
            float c = AttackVfxSizing.CounterScale(d), h = AttackVfxSizing.HaloScale(d);
            if (!float.IsFinite(c) || !float.IsFinite(h) || c < 1.65f || c > 2.65f || h < 1.6f || h > 3.13f)
                throw new Exception("Unsafe damage scale: " + d);
        }
        foreach (Vector2 origin in new[] { new Vector2(-500, 90), new Vector2(500, -90), Vector2.Zero })
        {
            foreach (I k in new[] { I.Shot, I.PoisonShot })
            {
                var node = new IroneyeAttackVfx { AttackKind = k, ShotOrigin = origin };
                if (node.ArrowTipAt(0).DistanceTo(origin) > .001f || node.ArrowTipAt(1).Length() > .001f)
                    throw new Exception("Arrow route changed.");
                node.Free();
            }
            foreach (R k in new[] { R.HaloOut, R.HaloReturn, R.BeastRock })
            foreach (decimal d in new[] { 8m, 200m })
            {
                var node = new RevenantAttackVfx { AttackKind = k, Source = origin, VisualDamage = d };
                if (node.FlightPoint(0).DistanceTo(origin) > .001f || node.FlightPoint(1).Length() > .001f)
                    throw new Exception("Prayer route changed.");
                node.Free();
            }
        }
    }

    private static void SetFrame(Node2D node, float t)
    {
        if (node is ParticleAttackVfx particle) particle.Seek(t);
        if (node is GuardianSample g) g.T = t;
        if (node is IroneyeSample i) i.T = t;
        if (node is RevenantSample r) r.T = t;
        if (node is MarkSample m) m.T = t;
        node.QueueRedraw();
    }

    private async void RunVerification()
    {
        _running = true;
        try
        {
            var viewport = new SubViewport { Size = new Vector2I(1600, 1200), TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            AddChild(viewport);
            foreach (var (name, make) in _cases)
            {
                var sample = make();
                sample.Position = new Vector2(800, 600);
                if (sample is RevenantAttackVfx rv) rv.Source = new Vector2(-380, 0);
                if (sample is IroneyeAttackVfx iv) iv.ShotOrigin = new Vector2(-380, 0);
                viewport.AddChild(sample);
                var emitters=sample.GetChildren().OfType<GpuParticles2D>().ToArray();
                (int Width,int Height,int Pixels) initialParticles=default;
                bool particlesMoved=false;
                Image? best = null;
                int pixels = 0, width = 0, height = 0;
                int readableSamples=0;
                for (int frame = 0; frame <= 120; frame++)
                {
                    SetFrame(sample, frame / 120f);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    if(emitters.Length>0 && (frame==24 || frame==60 || frame==96))
                    {
                        var footprint=await ParticleFootprint(viewport,sample,emitters);
                        if(initialParticles.Pixels<=20)initialParticles=footprint;
                        else particlesMoved|=footprint.Pixels>20 &&
                            (Math.Abs(footprint.Width-initialParticles.Width)>3 || Math.Abs(footprint.Height-initialParticles.Height)>3);
                    }
                    if (frame % 12 != 0 || frame == 0 || frame == 120) continue;
                    var img = viewport.GetTexture().GetImage();
                    var bounds = Bounds(img);
                    if(bounds.Pixels>=12000)readableSamples++;
                    if (bounds.Pixels > pixels)
                    {
                        best?.Dispose(); best = img;
                        (width, height, pixels) = bounds;
                    }
                    else img.Dispose();
                }
                if (best == null || pixels < 500) throw new Exception("Invisible effect: " + name);
                // Every attack is audited, not only the headline damage-scaled variants.
                bool passed=name.StartsWith("MARK /") ? pixels>=500 : width>=330 && height>=245 && pixels>=30000 && readableSamples>=4;
                if(!name.StartsWith("MARK /")&&(emitters.Length==0||!particlesMoved))throw new Exception("Native GPU particles did not move: "+name);
                _audit.Add(new {name,width,height,pixels,readableSamples,nativeEmitters=emitters.Length,particlesMoved,passed});
                if(!passed)_failures.Add(name);
                if (width >= 1590 || height >= 1190) throw new Exception("Clipped measurement: " + name);
                _captures.Add((name, best, width, height, pixels));
                GD.Print($"SIZE {name}: {width} x {height}; filled={pixels}");
                viewport.RemoveChild(sample); sample.Free();
            }
            viewport.QueueFree();
            foreach (G k in Enum.GetValues<G>()) Spawn(new GuardianAttackVfx { AttackKind = k, VisualDamage = 200m });
            foreach (I k in Enum.GetValues<I>()) Spawn(new IroneyeAttackVfx { AttackKind = k });
            foreach (R k in Enum.GetValues<R>()) Spawn(new RevenantAttackVfx { AttackKind = k, VisualDamage = 200m });
            for (int n = 0; n < 90; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (_lifecycle.Any(GodotObject.IsInstanceValid)) throw new Exception("Effect leaked past its lifetime.");
            foreach (int first in new[] { 22, 26 })
                for (int n = first + 1; n < first + 4; n++)
                    if (_captures[n].Width <= _captures[n - 1].Width || _captures[n].Pixels <= _captures[n - 1].Pixels)
                        throw new Exception("Damage intensity does not grow visibly.");
            File.WriteAllText(Path.Combine(_output, "measurements.json"), JsonSerializer.Serialize(
                _captures.Select(c => new { c.Name, c.Width, c.Height, c.Pixels }), new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(_output,"audit.json"),JsonSerializer.Serialize(_audit,new JsonSerializerOptions{WriteIndented=true}));
            // Contact sheets: all regular types, then dedicated four-tier comparisons.
            await SaveSheet("guardian_ironeye.png", Enumerable.Range(0, 9).ToArray(), 3, 3);
            await SaveSheet("revenant.png", Enumerable.Range(9, 13).ToArray(), 4, 4);
            await SaveSheet("damage_growth.png", Enumerable.Range(22, 8).ToArray(), 4, 2, 740, 760);
            await SaveSheet("mark_status.png",new[]{30,31},2,1);
            string nativePath = ProjectSettings.GlobalizePath("res://../../.tmp/vfx-scale/poison_012.png");
            if (File.Exists(nativePath))
            {
                var native = Image.LoadFromFile(nativePath);
                var bounds = Bounds(native);
                _captures.Add(("NATIVE / POISON APPLICATION", native, bounds.Width, bounds.Height, bounds.Pixels));
                await SaveSheet("native_comparison.png", new[] { 32, 0, 2, 32, 8, 9 }, 3, 2);
            }
            foreach (var capture in _captures) capture.Image.Dispose();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if(_failures.Count>0)GD.Print("AUDIT NEEDS REVISION: "+string.Join(", ",_failures));
            else GD.Print("PASS: all 30 attack variants meet area, filled-pixel and persistence standards; 2 marker states checked; 121 GPU frames each; endpoints and 22-node cleanup.");
            GetTree().Quit(_failures.Count==0?0:1);
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }

    private void Spawn(Node2D node)
    {
        node.Position = new Vector2(-2000, -2000); _lifecycle.Add(node); AddChild(node);
    }

    private async System.Threading.Tasks.Task<(int Width,int Height,int Pixels)> ParticleFootprint(SubViewport viewport,Node2D sample,GpuParticles2D[] emitters)
    {
        // CaptureRect GPU readback is unsupported by this GLES driver. Render the
        // real emitters alone instead, with their clocks frozen during capture.
        var sprites=sample.GetChildren().OfType<Sprite2D>().Select(s=>(Node:s,s.Visible)).ToArray();
        foreach(var sprite in sprites)sprite.Node.Visible=false;
        foreach(var emitter in emitters)emitter.SpeedScale=0;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        using var image=viewport.GetTexture().GetImage();
        var result=Bounds(image);
        foreach(var sprite in sprites)sprite.Node.Visible=sprite.Visible;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        return result;
    }

    private static (int Width, int Height, int Pixels) Bounds(Image image)
    {
        image.Convert(Image.Format.Rgba8);
        byte[] data = image.GetData();
        int w = image.GetWidth(), minX = w, minY = image.GetHeight(), maxX = -1, maxY = -1, count = 0;
        for (int p = 0; p < data.Length / 4; p++)
            if (data[p * 4 + 3] > 25)
            {
                int x = p % w, y = p / w;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); count++;
            }
        return (maxX - minX + 1, maxY - minY + 1, count);
    }

    private async System.Threading.Tasks.Task SaveSheet(string file, int[] indices, int columns, int rows, int cellHeight = 460, int cellWidth=640)
    {
        var view = new SubViewport { Size = new Vector2I(columns * cellWidth, rows * cellHeight + 65),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        AddChild(view);
        var sheet = new ContactSheet { Captures = indices.Select(i => _captures[i]).ToArray(), Columns = columns, CellHeight = cellHeight, CellWidth=cellWidth };
        view.AddChild(sheet);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = view.GetTexture().GetImage();
        if (image.SavePng(Path.Combine(_output, file)) != Error.Ok) throw new Exception("Capture failed.");
        view.QueueFree();
    }

    private async void RunMovie()
    {
        _running = true;
        try
        {
            string output = ProjectSettings.GlobalizePath("res://../../.tmp/vfx-scale/movie");
            var indices = Enumerable.Range(0,22).Concat(new[]{30,31}).ToArray();
            for(int page=0;page<4;page++)
            {
                string folder=Path.Combine(output,$"page{page+1}"); Directory.CreateDirectory(folder);
                var view=new SubViewport{Size=new Vector2I(1920,1080),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};
                AddChild(view);
                var board=new MovieBoard{Names=indices.Skip(page*6).Take(6).Select(i=>_cases[i].Name).ToArray()};view.AddChild(board);
                var samples=new List<Node2D>();
                for(int slot=0;slot<6;slot++)
                {
                    var sample=_cases[indices[page*6+slot]].Make();
                    sample.Position=new Vector2(350+slot%3*640,340+slot/3*500);sample.Scale=Vector2.One*.65f;
                    if(sample is IroneyeAttackVfx arrow)arrow.ShotOrigin=new Vector2(-210,0);
                    if(sample is RevenantAttackVfx prayer)prayer.Source=new Vector2(-210,0);
                    board.AddChild(sample);samples.Add(sample);
                }
                for(int frame=0;frame<48;frame++)
                {
                    float seconds=frame/24f;
                    foreach(var sample in samples)
                    {
                        float duration=sample switch { GuardianAttackVfx g=>g.Duration,IroneyeAttackVfx i=>i.Duration,RevenantAttackVfx r=>r.Duration,_=>1f};
                        SetFrame(sample,Math.Min(1,seconds/duration));
                    }
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    using var image=view.GetTexture().GetImage();image.Resize(1280,720,Image.Interpolation.Lanczos);
                    if(image.SavePng(Path.Combine(folder,$"frame_{frame:D2}.png"))!=Error.Ok)throw new Exception("Movie capture failed.");
                }
                RemoveChild(view);view.Free();GD.Print($"MOVIE page {page+1}: 48 frames captured");
            }
            GetTree().Quit();
        }
        catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }

    private void ShowLivePage()
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        for (int slot = 0; slot < 6; slot++)
        {
            int index = (_page * 6 + slot) % _cases.Count;
            var sample = _cases[index].Make();
            sample.Position = new Vector2(350 + slot % 3 * 640, 340 + slot / 3 * 490);
            sample.Scale = Vector2.One * .65f;
            AddChild(sample);
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_running) return;
        _time += (float)delta;
        foreach (Node2D child in GetChildren()) SetFrame(child, _time % 1.4f / 1f);
        if (Input.IsActionJustPressed("ui_accept")) { _page = (_page + 1) % ((_cases.Count+5)/6); ShowLivePage(); }
    }

    public override void _Draw()
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(25, 40), "ATTACK VFX / SPACE: NEXT PAGE / ALL CELLS AT 0.65 CANVAS SCALE", fontSize:24);
        for (int slot = 0; slot < 6; slot++)
        {
            Vector2 p = new(350 + slot % 3 * 640, 340 + slot / 3 * 490);
            DrawRect(new Rect2(p - new Vector2(48, 95), new Vector2(96, 150)), new Color("#353A49"));
            DrawString(ThemeDB.FallbackFont, p - new Vector2(315, 245), _cases[(_page * 6 + slot) % _cases.Count].Name, fontSize:22);
        }
    }
}

public partial class ContactSheet : Node2D
{
    public (string Name, Image Image, int Width, int Height, int Pixels)[] Captures = Array.Empty<(string, Image, int, int, int)>();
    public int Columns;
    public int CellHeight = 460;
    public int CellWidth = 640;
    public override void _Ready()
    {
        for (int i = 0; i < Captures.Length; i++)
            AddChild(new Sprite2D { Texture = ImageTexture.CreateFromImage(Captures[i].Image),
                Centered = false, Position = Origin(i), Scale = Vector2.One * .65f });
    }
    private Vector2 Origin(int i) => new Vector2(i % Columns * CellWidth + CellWidth*.5f, i / Columns * CellHeight + 100 + CellHeight * .5f)
        - (Vector2)Captures[i].Image.GetUsedRect().GetCenter() * .65f;
    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, Columns * CellWidth, ((Captures.Length + Columns - 1) / Columns) * CellHeight + 65), new Color("#161923"));
        DrawString(ThemeDB.FallbackFont, new Vector2(24, 38), "PRODUCTION VFX / SAME 0.65 SCALE IN EVERY CELL / DIMENSIONS IN COMBAT CANVAS PIXELS", fontSize:23);
        for (int i = 0; i < Captures.Length; i++)
        {
            Vector2 p = new(i % Columns * CellWidth, i / Columns * CellHeight + 65);
            DrawRect(new Rect2(p + Vector2.One * 6, new Vector2(CellWidth-12, CellHeight-12)), new Color(i % 2 == 0 ? "#252A38" : "#292E3D"));
            Vector2 target = Origin(i) + (Vector2)Captures[i].Image.GetSize() * .325f;
            DrawRect(new Rect2(target - new Vector2(48, 95), new Vector2(96, 150)), new Color("#414755"));
            DrawString(ThemeDB.FallbackFont, p + new Vector2(22, 33), Captures[i].Name, fontSize:23);
            DrawString(ThemeDB.FallbackFont, p + new Vector2(22, 61), $"{Captures[i].Width} x {Captures[i].Height} / alpha > 0.10", fontSize:18, modulate: new Color("#B0BBCD"));
        }
    }
}

public partial class MovieBoard : Node2D
{
    public string[] Names=Array.Empty<string>();
    public override void _Draw()
    {
        DrawRect(new Rect2(0,0,1920,1080),new Color("#161923"));
        DrawString(ThemeDB.FallbackFont,new Vector2(24,38),"ALL EFFECTS REMADE / PRODUCTION DRAWING / 0.65 CANVAS SCALE / 24 FPS",fontSize:26);
        for(int i=0;i<Names.Length;i++)
        {
            Vector2 corner=new(i%3*640,i/3*500+65),target=new(350+i%3*640,340+i/3*500);
            DrawRect(new Rect2(corner+Vector2.One*6,new Vector2(628,488)),new Color("#252A38"));
            DrawRect(new Rect2(target-new Vector2(48,95),new Vector2(96,150)),new Color("#414755"));
            DrawString(ThemeDB.FallbackFont,corner+new Vector2(22,36),Names[i],fontSize:26);
        }
    }
}
