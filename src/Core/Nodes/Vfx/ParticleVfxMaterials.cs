#nullable enable
using System;
using System.Collections.Generic;
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Generated luminosity masks. Black represents zero coverage, not a visible background.</summary>
public static class ParticleVfxMaterials
{
    public static string AssetRoot { get; set; } = "res://images/vfx/particle_remake/";
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static Shader? _shader;
    public static Texture2D Texture(int tile)
    {
        string name = "refined.png";
        string path = AssetRoot + name;
        if (!Textures.TryGetValue(path, out var texture))
        {
            if (path.StartsWith("res://")) texture = GD.Load<Texture2D>(path);
            else { using var image = Image.LoadFromFile(path); texture = ImageTexture.CreateFromImage(image); }
            Textures[path] = texture ?? throw new InvalidOperationException("Missing VFX atlas: " + path);
        }
        return texture;
    }
    public static ShaderMaterial Material(int tile, Color tint, float flow = .01f)
    {
        _shader ??= new Shader { Code = ShaderCode };
        var material = new ShaderMaterial { Shader = _shader };
        material.SetShaderParameter("tile_rect", new Vector4((tile % 4) * .25f, (tile / 4) * .25f, .25f, .25f));
        material.SetShaderParameter("tint", tint);
        material.SetShaderParameter("flow", Math.Min(flow, .008f));
        return material;
    }
    // Explicit phase makes captures reproducible and pauses follow the effect's own clock.
    public const string ShaderCode = """
shader_type canvas_item;
render_mode unshaded, blend_mix;
uniform vec4 tile_rect = vec4(0.0, 0.0, 0.25, 0.25);
uniform vec4 tint : source_color = vec4(1.0);
uniform float phase = 0.0;
uniform float dissolve = 0.0;
uniform float flow = 0.01;
uniform float gain = 1.0;
varying vec4 vertex_color;
void vertex() { vertex_color = COLOR; }
void fragment() {
    vec2 p=UV;
    // Broad silhouette motion; no granular erosion or thin turbulent filaments.
    p += vec2(sin(p.y*5.0+phase),cos(p.x*4.0-phase)) * flow;
    vec2 edge=smoothstep(vec2(0.0),vec2(.025),p)*(1.0-smoothstep(vec2(.975),vec2(1.0),p));
    vec3 sample_color=texture(TEXTURE,tile_rect.xy+clamp(p,vec2(.001),vec2(.999))*tile_rect.zw).rgb;
    float value=max(sample_color.r,max(sample_color.g,sample_color.b));
    // Preserve the authored broad shading and bright core, without filament noise.
    float coverage=smoothstep(.045,.3,value)*sqrt(max(value,0.0));
    vec3 body=mix(tint.rgb*.48,tint.rgb,smoothstep(.15,.72,value));
    vec3 color=mix(body,mix(tint.rgb,vec3(1.0),.3),smoothstep(.76,1.0,value))*min(gain,1.15);
    float fade=1.0-smoothstep(.1,1.0,dissolve);
    COLOR=vec4(color,coverage*.82*edge.x*edge.y*fade*tint.a)*vertex_color;
}
""";
}
