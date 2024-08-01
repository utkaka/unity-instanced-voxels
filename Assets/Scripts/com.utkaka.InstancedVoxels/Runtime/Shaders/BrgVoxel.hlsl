void get_voxel_animation_position_float (
    const in float3 vertex_position,
    const in float voxel_size,
    const in float3 start_position,
    in float current_animation_frame,
    in float next_animation_frame,
    const in float animation_lerp_ratio,
    const in float bones_count,
    const in float frames_count,
    const in float position_bone,
    out float3 voxel_position_out
    ) {
    const int position_bone_int = (int)position_bone;
    const float3 voxel_position = float3((position_bone_int & 65280u) >> 8, (position_bone_int & 16711680u) >> 16, (position_bone_int & 4278190080u) >> 24);
    voxel_position_out = voxel_position * voxel_size + start_position + vertex_position;
}

void get_voxel_color_float (const in float voxel_color, out float4 color) {
    const int voxel_color_int = (int)voxel_color;
    half3 input_color = half3(half(voxel_color_int & 255u), half((voxel_color_int & 65280u) >> 8), half((voxel_color_int & 16711680u) >> 16)) / half(255);
    // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    input_color = input_color * (input_color * (input_color * half(0.305306011) + half(0.682171111)) + half(0.012522878));
    color = half4(input_color.x, input_color.y, input_color.z, half(1));
}