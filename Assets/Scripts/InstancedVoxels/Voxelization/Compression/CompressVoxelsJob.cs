using InstancedVoxels.Voxelization.Sat;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace InstancedVoxels.Voxelization.Compression {
	[BurstCompile]
	public struct CompressVoxelsJob : IJobFor{
		[ReadOnly]
		private NativeArray<SatVoxel> _satVoxels;
		[WriteOnly]
		private NativeList<CompressedVoxel> _compressedVoxels;
		[WriteOnly]
		private NativeList<int> _voxelIndices;

		public CompressVoxelsJob(NativeArray<SatVoxel> satVoxels, NativeList<CompressedVoxel> compressedVoxels,
			NativeList<int> voxelIndices) {
			_satVoxels = satVoxels;
			_compressedVoxels = compressedVoxels;
			_voxelIndices = voxelIndices;
		}

		public void Execute(int index) {
			var satVoxel = _satVoxels[index];
			if (satVoxel.MeshIndex == 0) return;
			_compressedVoxels.AddNoResize(new CompressedVoxel(satVoxel));
			_voxelIndices.AddNoResize(index);
		}
	}
}