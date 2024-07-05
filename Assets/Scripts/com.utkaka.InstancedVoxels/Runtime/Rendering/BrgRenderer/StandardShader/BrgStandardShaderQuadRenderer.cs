using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader {
    public class BrgStandardShaderQuadRenderer : BrgQuadRenderer {
        private static readonly int ShaderObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        private static readonly int  ShaderWorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        private static readonly int  ShaderColorID = Shader.PropertyToID("_BaseColor");
        
        public BrgStandardShaderQuadRenderer(int sideIndex, float voxelSize, Vector3 startPosition, int bonesCount,
            int animationLength, Material material, VoxelsBox box, NativeArray<ShaderVoxel> voxels,
            NativeArray<byte> voxelBoxMasks, NativeArray<float3> bonePositionsArray,
            NativeArray<float3> boneAnimationPositionsArray, NativeArray<float4> boneAnimationRotationsArray) : base(
            sideIndex, voxelSize, startPosition, bonesCount, animationLength, material, box, voxels, voxelBoxMasks,
            bonePositionsArray, boneAnimationPositionsArray, boneAnimationRotationsArray) {
        }

        protected override NativeArray<MetadataValue> CreateMetadata(int positionsCount) {
            var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            batchMetadata[0] = CreateMetadataValue(ShaderObjectToWorldID, 0, true);       // matrices
            batchMetadata[1] = CreateMetadataValue(ShaderWorldToObjectID, positionsCount * 3 * 16, true); // inverse matrices
            batchMetadata[2] = CreateMetadataValue(ShaderColorID, positionsCount * 3 * 2 * 16, true); // colors
            return batchMetadata;
        }
    }
}