using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    public class BrgCustomShaderQuadRenderer : BrgQuadRenderer {
        private static readonly int ShaderObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
	    private static readonly int ShaderWorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
	    private static readonly int  ShaderPositionBoneID = Shader.PropertyToID("_PositionBone");
	    private static readonly int  ShaderColorID = Shader.PropertyToID("_Color");

		public BrgCustomShaderQuadRenderer(int sideIndex, float voxelSize, Vector3 startPosition, int bonesCount,
			int animationLength, Material material, VoxelsBox box, NativeArray<ShaderVoxel> voxels,
			NativeArray<byte> voxelBoxMasks, NativeArray<float3> bonePositionsArray,
			NativeArray<float3> boneAnimationPositionsArray, NativeArray<float4> boneAnimationRotationsArray) : base(
			sideIndex, voxelSize, startPosition, bonesCount, animationLength, material, box, voxels, voxelBoxMasks,
			bonePositionsArray, boneAnimationPositionsArray, boneAnimationRotationsArray) {
		}

		protected override NativeArray<MetadataValue> CreateMetadata(int positionsCount) {
			var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			batchMetadata[0] = CreateMetadataValue(ShaderPositionBoneID, 0, true);       // positions with bone
			batchMetadata[1] = CreateMetadataValue(ShaderColorID, positionsCount * 4, true); // colors
			batchMetadata[2] = CreateMetadataValue(ShaderObjectToWorldID, positionsCount * 8, false);       // matrices
			batchMetadata[3] = CreateMetadataValue(ShaderWorldToObjectID, positionsCount * 8 + 3 * 16, false);
			return batchMetadata;
		}
    }
}