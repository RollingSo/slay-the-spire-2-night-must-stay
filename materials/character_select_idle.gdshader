shader_type canvas_item;

uniform vec2 body_center = vec2(0.65, 0.55);
uniform vec2 body_radius = vec2(0.28, 0.48);
uniform vec2 secondary_center = vec2(0.42, 0.62);
uniform vec2 secondary_radius = vec2(0.16, 0.33);
uniform float breath_amount : hint_range(0.0, 0.01) = 0.0025;
uniform float secondary_amount : hint_range(0.0, 0.02) = 0.004;
uniform float idle_speed : hint_range(0.1, 3.0) = 0.72;

float ellipse_mask(vec2 uv, vec2 center, vec2 radius) {
	vec2 normalized = (uv - center) / max(radius, vec2(0.001));
	return 1.0 - smoothstep(0.62, 1.0, length(normalized));
}

void fragment() {
	float phase = sin(TIME * idle_speed);
	float delayed_phase = sin(TIME * idle_speed + 0.72);
	float body_mask = ellipse_mask(UV, body_center, body_radius);
	float secondary_mask = ellipse_mask(UV, secondary_center, secondary_radius);

	vec2 sample_uv = UV;
	sample_uv.y += phase * breath_amount * body_mask;
	sample_uv.x += delayed_phase * secondary_amount * secondary_mask * (UV.y - secondary_center.y);
	sample_uv.y -= abs(delayed_phase) * secondary_amount * 0.18 * secondary_mask;

	COLOR = texture(TEXTURE, clamp(sample_uv, vec2(0.001), vec2(0.999)));
}
