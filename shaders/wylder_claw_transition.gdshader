shader_type canvas_item;
render_mode unshaded;
uniform float threshold : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    // Three staggered claw cuts sweep diagonally across the viewport.
    float diagonal = UV.x * 0.76 + UV.y * 0.24;
    float lane = floor(UV.y * 3.0);
    float edge = threshold * 1.5 - 0.22 - lane * 0.065;
    float cover = 1.0 - smoothstep(edge - 0.018, edge, diagonal);
    float slash = (1.0 - smoothstep(0.0, 0.012, abs(diagonal - edge)))
        * smoothstep(0.02, 0.13, threshold) * (1.0 - smoothstep(0.75, 0.97, threshold));
    COLOR = vec4(vec3(0.95, 0.48, 0.12) * slash, max(cover, slash));
    if (threshold <= 0.0) { COLOR = vec4(0.0); }
    if (threshold >= 1.0) { COLOR = vec4(0.0, 0.0, 0.0, 1.0); }
}
