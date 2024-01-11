struct Voxel {
    uint position_bone;
    uint color;
};

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<Voxel> voxels_buffer;
StructuredBuffer<float3> bone_positions_buffer;
StructuredBuffer<float3> bone_positions_animation_buffer;
StructuredBuffer<float4> bone_rotations_animation_buffer;
#endif


float3 voxel_position;
int voxel_bone;
float4 voxel_color;

void configure_procedural () {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    Voxel voxel = voxels_buffer[unity_InstanceID];
    voxel_position = float3((voxel.position_bone & 65280u) >> 8, (voxel.position_bone & 16711680u) >> 16, (voxel.position_bone & 4278190080u) >> 24);
    voxel_bone = voxel.position_bone & 255u;
    float3 input_color = float3(voxel.color & 255u, (voxel.color & 65280u) >> 8, (voxel.color & 16711680u) >> 16) / 255;
    // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    input_color = input_color * (input_color * (input_color * 0.305306011 + 0.682171111) + 0.012522878);
    voxel_color = float4(input_color.x, input_color.y, input_color.z, 1);
    #endif
}

float3 mul_quaternion(float4 q, const float3 v) {
    const float3 t = 2 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

void get_voxel_animation_position_float (
    const in float3 vertex_position,
    const in float voxel_size,
    const in float3 start_position,
    in float current_animation_frame,
    in float next_animation_frame,
    const in float animation_lerp_ratio,
    const in float bones_count,
    const in float frames_count,
    out float3 voxel_position_out
    ) {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    const float3 voxel_vertex_position = voxel_position * voxel_size + start_position + vertex_position;
    current_animation_frame = current_animation_frame + voxel_bone * frames_count;
    next_animation_frame = next_animation_frame + voxel_bone * frames_count;
    
    const float3 animation_position = lerp(bone_positions_animation_buffer[current_animation_frame], bone_positions_animation_buffer[next_animation_frame], animation_lerp_ratio);
    const float4 animation_rotation = lerp(bone_rotations_animation_buffer[current_animation_frame], bone_rotations_animation_buffer[next_animation_frame], animation_lerp_ratio);
    const float3 bone_position = bone_positions_buffer[voxel_bone];
    const float3 offset_point = voxel_vertex_position - bone_position;
    const float3 rotated_point = mul_quaternion(animation_rotation, offset_point) + bone_position;
    voxel_position_out = rotated_point + animation_position;
    #else
    voxel_position_out = vertex_position;
    #endif
}

void get_voxel_color_float (out float4 color) {
    color = voxel_color;
}