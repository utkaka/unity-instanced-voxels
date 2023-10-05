using InstancedVoxels.Voxelization.Compression;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace InstancedVoxels.Voxelization.Weights {
	[BurstCompile]
	public struct CalculateVoxelWeightsJob : IJobParallelFor {
		[ReadOnly]
		private NativeArray<CompressedVoxel> _compressedVoxels;
		[WriteOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;

		public CalculateVoxelWeightsJob(NativeArray<CompressedVoxel> compressedVoxels, NativeArray<WeightedVoxel> weightedVoxels) {
			_compressedVoxels = compressedVoxels;
			_weightedVoxels = weightedVoxels;
		}

		public void Execute(int index) {
			var compressedVoxel = _compressedVoxels[index];
			var f0 = compressedVoxel.VertexPosition0 - compressedVoxel.VoxelCenter;
			var f1 = compressedVoxel.VertexPosition1 - compressedVoxel.VoxelCenter;
			var f2 = compressedVoxel.VertexPosition2 - compressedVoxel.VoxelCenter;
			var area0 = math.length(math.cross(f1, f2));
			var area1 = math.length(math.cross(f2, f0));
			var area2 = math.length(math.cross(f0, f1));
			var area = area0 + area1 + area2;
			_weightedVoxels[index] = new WeightedVoxel(compressedVoxel, area0 / area, area1 / area, area2 / area);
		}
	}
}