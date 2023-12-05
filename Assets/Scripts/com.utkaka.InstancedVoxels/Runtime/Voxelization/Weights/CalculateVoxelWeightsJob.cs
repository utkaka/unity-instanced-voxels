using com.utkaka.InstancedVoxels.Runtime.Voxelization.Sat;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.Weights {
	[BurstCompile]
	public struct CalculateVoxelWeightsJob : IJobParallelFor {
		[ReadOnly]
		private NativeArray<SatVoxel> _satVoxels;
		[WriteOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;

		public CalculateVoxelWeightsJob(NativeArray<SatVoxel> satVoxels, NativeArray<WeightedVoxel> weightedVoxels) {
			_satVoxels = satVoxels;
			_weightedVoxels = weightedVoxels;
		}

		public void Execute(int index) {
			var satVoxel = _satVoxels[index];
			var f0 = satVoxel.VertexPosition0 - satVoxel.VoxelCenter;
			var f1 = satVoxel.VertexPosition1 - satVoxel.VoxelCenter;
			var f2 = satVoxel.VertexPosition2 - satVoxel.VoxelCenter;
			var area0 = math.length(math.cross(f1, f2));
			var area1 = math.length(math.cross(f2, f0));
			var area2 = math.length(math.cross(f0, f1));
			var area = area0 + area1 + area2;
			_weightedVoxels[index] = new WeightedVoxel(satVoxel, area0 / area, area1 / area, area2 / area);
		}
	}
}