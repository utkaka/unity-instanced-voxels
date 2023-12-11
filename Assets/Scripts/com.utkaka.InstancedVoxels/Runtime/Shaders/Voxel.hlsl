#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<float3> voxel_positions_buffer;
StructuredBuffer<uint> voxel_bones_buffer;
StructuredBuffer<float3> voxel_colors_buffer;
StructuredBuffer<float3> bone_positions_buffer;
StructuredBuffer<float3> bone_positions_animation_buffer;
StructuredBuffer<float4> bone_rotations_animation_buffer;
#endif


float3 voxel_position;
uint voxel_bone;
float4 voxel_color;

void configure_procedural () {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    #if SHADER_STAGE_VERTEX
    voxel_position = voxel_positions_buffer[unity_InstanceID];
    voxel_bone = voxel_bones_buffer[unity_InstanceID];
    #endif
    #if SHADER_STAGE_FRAGMENT
    const float3 input_color = voxel_colors_buffer[unity_InstanceID];
    voxel_color = float4(input_color.x, input_color.y, input_color.z, 1);
    #endif
    #endif
}

float3 mul_quaternion(float4 q, const float3 v) {
    const float3 t = 2 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

void get_voxel_animation_position_float (
    const in float3 vertex_position,
    const in float animation_frame,
    const in float bones_count,
    const in float frames_count,
    out float3 voxel_position_out
    ) {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    const float3 voxel_vertex_position = voxel_position + vertex_position;
    uint animation_index = animation_frame;
    const float lerp_ratio = animation_frame - animation_index;
    const uint next_animation_index = (animation_index + 1) % frames_count + voxel_bone * frames_count;
    animation_index = animation_index + voxel_bone * frames_count;
    
    const float3 animation_position = lerp(bone_positions_animation_buffer[animation_index], bone_positions_animation_buffer[next_animation_index], lerp_ratio);
    const float4 animation_rotation = lerp(bone_rotations_animation_buffer[animation_index], bone_rotations_animation_buffer[next_animation_index], lerp_ratio);
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

/*void get_voxel_position_float (
    const in float3 vertex_position,
    const in UnityTexture2D voxel_texture_2d,
    const in float voxel_texture_size,
    out float3 voxel_position
    ) {
    const float texture_x = instance_id % voxel_texture_size;
    const float texture_y = (instance_id - texture_x) / voxel_texture_size;
    voxel_position = SAMPLE_TEXTURE2D_LOD(voxel_texture_2d, voxel_texture_2d.samplerstate, float2(texture_x / voxel_texture_size, texture_y / voxel_texture_size), 0) + vertex_position;
}*/