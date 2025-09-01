shader_type spatial;
render_mode blend_add, depth_draw_never, cull_disabled, unshaded;

varying vec4 custom_data; // MultiMesh 的 CustomData (R=progress, GBA=block_pos)

uniform vec3 crack_color = vec3(0.0, 0.0, 0.0); // 裂纹颜色（黑色）
uniform float crack_thickness = 0.02;
uniform float noise_scale = 10.0;

float hash(vec2 p) {
	return fract(sin(dot(p + vec2(custom_data.g, custom_data.b + custom_data.a), vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	float a = hash(i + vec2(0.0, 0.0));
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

void fragment() {
	float damage_progress = custom_data.r;
	if (damage_progress <= 0.0) {
		discard;
	}
	vec2 scaled_uv = UV * noise_scale;
	float n = noise(scaled_uv);
	float threshold = 0.5 - damage_progress * 0.5;
	float intensity = smoothstep(threshold - crack_thickness, threshold + crack_thickness, n) * damage_progress;
	ALBEDO = crack_color * intensity;
	ALPHA = intensity;
}