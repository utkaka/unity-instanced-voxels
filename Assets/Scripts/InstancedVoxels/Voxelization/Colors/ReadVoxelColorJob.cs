using InstancedVoxels.MeshData;
using InstancedVoxels.Voxelization.Compression;
using InstancedVoxels.Voxelization.Sat;
using InstancedVoxels.Voxelization.Weights;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.Voxelization.Colors {
	[BurstCompile]
	public struct ReadVoxelColorJob : IJobParallelFor {
		[ReadOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<VertexUVReader> _vertexUVReaders;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<TextureDescriptor> _textureDescriptors;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<Color> _textures;
		[WriteOnly]
		private NativeArray<float3> _voxelColors;

		public ReadVoxelColorJob(NativeArray<WeightedVoxel> weightedVoxels, NativeArray<VertexUVReader> vertexUVReaders, NativeArray<TextureDescriptor> textureDescriptors, NativeArray<Color> textures, NativeArray<float3> voxelColors) {
			_textureDescriptors = textureDescriptors;
			_vertexUVReaders = vertexUVReaders;
			_weightedVoxels = weightedVoxels;
			_textures = textures;
			_voxelColors = voxelColors;
		}

		public void Execute(int index) {
			var weightedVoxel = _weightedVoxels[index];
			var meshIndex = weightedVoxel.MeshIndex;
			var uvReader = _vertexUVReaders[meshIndex];
			var uv0 = uvReader.GetVertexUV(weightedVoxel.VertexIndex0);
			var uv1 = uvReader.GetVertexUV(weightedVoxel.VertexIndex1);
			var uv2 = uvReader.GetVertexUV(weightedVoxel.VertexIndex2);
			var uv = uv0 * weightedVoxel.VertexWeight0 + uv1 * weightedVoxel.VertexWeight1 + uv2 * weightedVoxel.VertexWeight2;
			var color = _textures[_textureDescriptors[meshIndex].GetUvIndex(uv)];
			_voxelColors[index] = new float3(color.r, color.g, color.b);
		}
	}
}