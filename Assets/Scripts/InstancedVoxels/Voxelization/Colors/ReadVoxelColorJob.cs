using InstancedVoxels.MeshData;
using InstancedVoxels.VoxelData;
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
		private NativeArray<Color32> _textures;
		[WriteOnly]
		private NativeArray<byte3> _voxelColors;
		[WriteOnly]
		private NativeArray<bool> _voxelColored;

		public ReadVoxelColorJob(NativeArray<WeightedVoxel> weightedVoxels, NativeArray<VertexUVReader> vertexUVReaders,
			NativeArray<TextureDescriptor> textureDescriptors, NativeArray<Color32> textures,
			NativeArray<byte3> voxelColors, NativeArray<bool> voxelColored) {
			_textureDescriptors = textureDescriptors;
			_vertexUVReaders = vertexUVReaders;
			_weightedVoxels = weightedVoxels;
			_textures = textures;
			_voxelColors = voxelColors;
			_voxelColored = voxelColored;
		}

		public void Execute(int index) {
			var weightedVoxel = _weightedVoxels[index];
			var meshIndex = weightedVoxel.MeshIndex - 1;
			if (meshIndex < 0) return;
			var uvReader = _vertexUVReaders[meshIndex];
			var uv0 = uvReader.GetVertexUV(weightedVoxel.VertexIndex0);
			var uv1 = uvReader.GetVertexUV(weightedVoxel.VertexIndex1);
			var uv2 = uvReader.GetVertexUV(weightedVoxel.VertexIndex2);
			var uv = uv0 * weightedVoxel.VertexWeight0 + uv1 * weightedVoxel.VertexWeight1 + uv2 * weightedVoxel.VertexWeight2;
			var color = _textures[_textureDescriptors[meshIndex].GetUvIndex(uv)];
			_voxelColors[index] = new byte3(color.r, color.g, color.b);
			_voxelColored[index] = true;
		}
	}
}