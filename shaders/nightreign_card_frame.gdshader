shader_type canvas_item;

uniform vec4 frame_shadow : source_color = vec4(0.08, 0.10, 0.09, 1.0);
uniform vec4 frame_mid : source_color = vec4(0.41, 0.45, 0.29, 1.0);
uniform vec4 frame_highlight : source_color = vec4(0.78, 0.85, 0.29, 1.0);
uniform vec4 panel_shadow : source_color = vec4(0.08, 0.10, 0.09, 1.0);
uniform vec4 panel_mid : source_color = vec4(0.27, 0.31, 0.22, 1.0);
uniform float frame_saturation_threshold : hint_range(0.0, 1.0) = 0.16;
uniform float contrast : hint_range(0.5, 2.0) = 1.08;

varying vec4 modulate_color;

void vertex() {
	modulate_color = COLOR;
}

void fragment() {
	vec4 source = texture(TEXTURE, UV);
	float value = dot(source.rgb, vec3(0.2989, 0.5870, 0.1140));
	float maximum = max(source.r, max(source.g, source.b));
	float minimum = min(source.r, min(source.g, source.b));
	float chroma = maximum - minimum;

	float shaped_value = clamp((value - 0.5) * contrast + 0.5, 0.0, 1.0);
	float middle_mix = smoothstep(0.03, 0.34, shaped_value);
	float highlight_mix = smoothstep(0.30, 0.58, shaped_value);
	float frame_mix = smoothstep(
		frame_saturation_threshold * 0.55,
		frame_saturation_threshold * 1.55,
		chroma
	);

	vec3 themed_frame = mix(frame_shadow.rgb, frame_mid.rgb, middle_mix);
	themed_frame = mix(themed_frame, frame_highlight.rgb, highlight_mix);
	vec3 themed_panel = mix(
		panel_shadow.rgb,
		panel_mid.rgb,
		smoothstep(0.04, 0.42, shaped_value)
	);

	vec3 themed = mix(themed_panel, themed_frame, frame_mix);
	COLOR = vec4(themed, source.a) * modulate_color;
}
