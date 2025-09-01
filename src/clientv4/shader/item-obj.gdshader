shader_type spatial;
render_mode cull_disabled, unshaded; // 禁用背面剔除，实现双面渲染；unshaded避免默认光照影响
uniform sampler2D albedo_texture : source_color, filter_nearest; // 大Atlas纹理，source_color保留Alpha，filter_nearest避免模糊
instance uniform vec4 atlas_rect : hint_range(0.0, 1.0) = vec4(0.0, 0.0, 1.0, 1.0); // Atlas子区域：vec4(offset_x, offset_y, scale_x, scale_y)
uniform float alpha_scissor_threshold : hint_range(0.0, 1.0) = 0.9; // Alpha剪切阈值
void fragment() {
    vec2 modified_uv = UV; // 从UV开始（Sprite3D的UV是0-1范围）

    // 背面镜像翻转（水平）
    if (!FRONT_FACING) {
        modified_uv.x = 1.0 - modified_uv.x;
    }

    // 应用Atlas偏移和缩放：将UV映射到子区域
    modified_uv = modified_uv * atlas_rect.zw + atlas_rect.xy;

    // 采样纹理
    vec4 tex_color = texture(albedo_texture, modified_uv);

    // 设置输出
    ALBEDO = tex_color.rgb;
    ALPHA = tex_color.a;

    // Alpha Scissor：根据阈值剪切透明部分
    ALPHA_SCISSOR_THRESHOLD = alpha_scissor_threshold;
}