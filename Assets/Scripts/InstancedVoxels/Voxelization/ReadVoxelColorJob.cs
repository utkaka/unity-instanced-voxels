using InstancedVoxels.MeshData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.Voxelization {
	[BurstCompile]
	public struct ReadVoxelColorJob : IJobParallelFor {
		[ReadOnly]
		private NativeArray<SatVoxel> _satVoxels;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<VertexUVReader> _vertexUVReaders;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<TextureDescriptor> _textureDescriptors;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<Color> _textures;
		[WriteOnly]
		private NativeArray<Color> _voxelColors;

		public ReadVoxelColorJob(NativeArray<SatVoxel> satVoxels, NativeArray<VertexUVReader> vertexUVReaders, NativeArray<TextureDescriptor> textureDescriptors, NativeArray<Color> textures, NativeArray<Color> voxelColors) {
			_textureDescriptors = textureDescriptors;
			_vertexUVReaders = vertexUVReaders;
			_satVoxels = satVoxels;
			_textures = textures;
			_voxelColors = voxelColors;
		}

		public void Execute(int index) {
			var satVoxel = _satVoxels[index];
			if (satVoxel.MeshIndex == 0) return;
			var meshIndex = satVoxel.MeshIndex - 1;
			var uvReader = _vertexUVReaders[meshIndex];
			var uv0 = uvReader.GetVertexUV(satVoxel.VertexIndex0);
			var uv1 = uvReader.GetVertexUV(satVoxel.VertexIndex1);
			var uv2 = uvReader.GetVertexUV(satVoxel.VertexIndex2);
			var f0 = satVoxel.VertexPosition0 - satVoxel.VoxelCenter;
			var f1 = satVoxel.VertexPosition1 - satVoxel.VoxelCenter;
			var f2 = satVoxel.VertexPosition2 - satVoxel.VoxelCenter;
			var area0 = math.length(math.cross(f1, f2));
			var area1 = math.length(math.cross(f2, f0));
			var area2 = math.length(math.cross(f0, f1));
			var area = area0 + area1 + area2;
			var uv = (uv0 * area0 + uv1 * area1 + uv2 * area2) / area;
			_voxelColors[index] = _textures[_textureDescriptors[meshIndex].GetUvIndex(uv)];
		}
	}
}