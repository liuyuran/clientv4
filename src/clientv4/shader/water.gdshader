shader_type spatial;

// Water properties
uniform vec4 water_color : source_color = vec4(0.1, 0.4, 0.7, 0.7);
uniform vec4 deep_water_color : source_color = vec4(0.05, 0.2, 0.5, 0.8);
uniform sampler2D noise_texture;
uniform sampler2D noise_texture2;
uniform float time_scale = 1.0;
uniform float wave_strength = 0.1;
uniform float wave_speed = 0.5;
uniform float refraction = 0.05;

varying vec3 vertex_pos;

void vertex() {
    vertex_pos = VERTEX;
    
    // Animate vertices for waves
    float time = TIME * wave_speed;
    vec2 uv = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz * 0.1;
    float noise_val = texture(noise_texture, uv + vec2(time * 0.1, time * 0.2)).r;
    float noise_val2 = texture(noise_texture2, uv * 1.5 - vec2(time * 0.15, time * 0.1)).r;
    float combined = (noise_val + noise_val2) * 0.5;
    
    VERTEX.y += combined * wave_strength;
    
    // Adjust normals based on wave height
    NORMAL = normalize(vec3(noise_val * 0.5 - 0.25, 1.0, noise_val2 * 0.5 - 0.25));
}

void fragment() {
    // Calculate flow animation
    float time = TIME * time_scale;
    vec2 flow_uv = vertex_pos.xz * 0.1;
    
    // Sample noise with time offsets for flow effect
    float noise1 = texture(noise_texture, flow_uv + time * 0.05).r;
    float noise2 = texture(noise_texture2, flow_uv * 1.2 - time * 0.04).r;
    
    // Add depth variation
    float depth_factor = noise1 * 0.5 + 0.5;
    
    // Add ripples and waves
    float ripple = abs(noise2 * 2.0 - 1.0);
    ripple = 1.0 - smoothstep(0.2, 0.6, ripple);
    
    // Color mixing
    vec3 final_color = mix(water_color.rgb, deep_water_color.rgb, depth_factor);
    final_color = mix(final_color, vec3(1.0), ripple * 0.1); // Add foam/highlights
    
    ALBEDO = final_color;
    ROUGHNESS = 0.1;
    SPECULAR = 0.7;
    ALPHA = mix(water_color.a, deep_water_color.a, depth_factor);
    
    // Simple refraction
    NORMAL_MAP = vec3(noise1 * 2.0 - 1.0, noise2 * 2.0 - 1.0, 1.0) * refraction;
}