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
        string name = tile < 4 ? "elemental.png" : tile < 8 ? "energy.png" : tile < 12 ? "physical.png" : "motion.png";
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
        int quadrant = tile % 4;
        var material = new ShaderMaterial { Shader = _shader };
        material.SetShaderParameter("tile_rect", new Vector4((quadrant % 2) * .5f, (quadrant / 2) * .5f, .5f, .5f));
        material.SetShaderParameter("tint", tint);
        material.SetShaderParameter("flow", flow);
        return material;
    }
    // Explicit phase makes captures reproducible and pauses follow the effect's own clock.
    public const string ShaderCode = """
shader_type canvas_item;
render_mode unshaded, blend_mix;
uniform vec4 tile_rect = vec4(0.0, 0.0, 0.5, 0.5);
uniform vec4 tint : source_color = vec4(1.0);
uniform float phase = 0.0;
uniform float dissolve = 0.0;
uniform float flow = 0.01;
uniform float gain = 1.0;
varying vec4 vertex_color;
void vertex() { vertex_color = COLOR; }
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p) {
    vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void fragment() {
    vec2 p=UV;
    float n=noise(p*7.0+vec2(phase*.65,-phase*.85));
    p += vec2(n-.5,noise(p*8.0+phase*.3)-.5)*flow;
    vec2 edge=smoothstep(vec2(0.0),vec2(.025),p)*(1.0-smoothstep(vec2(.975),vec2(1.0),p));
    vec3 sample_color=texture(TEXTURE,tile_rect.xy+clamp(p,vec2(.001),vec2(.999))*tile_rect.zw).rgb;
    float value=max(sample_color.r,max(sample_color.g,sample_color.b));
    float coverage=pow(max(0.0,(value-.018)/.982),.72);
    float erosion=1.0-smoothstep(n*.7,n*.7+.3,dissolve);
    vec3 color=mix(tint.rgb*.62, mix(tint.rgb,vec3(1),.35),smoothstep(.12,.95,value));
    COLOR=vec4(color,clamp(coverage*gain,0.0,1.0)*edge.x*edge.y*erosion*tint.a)*vertex_color;
}
""";
}
