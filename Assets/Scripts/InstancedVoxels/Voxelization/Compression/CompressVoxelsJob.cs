using InstancedVoxels.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace InstancedVoxels.Voxelization.Compression {
	[BurstCompile]
	public struct CompressVoxelsJob : IJobFor {
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		[ReadOnly]
		private NativeArray<int> _voxelBones;
		[ReadOnly]
		private NativeArray<VoxelColor32> _voxelColors;
		[WriteOnly]
		private NativeList<int> _compressedIndices;
		[WriteOnly]
		private NativeList<int> _compressedBones;
		[WriteOnly]
		private NativeList<VoxelColor32> _compressedColors;

		public CompressVoxelsJob(NativeArray<bool> outerVoxels, NativeArray<int> voxelBones,
			NativeArray<VoxelColor32> voxelColors, NativeList<int> compressedIndices, NativeList<int> compressedBones,
			NativeList<VoxelColor32> compressedColors) : this() {
			_outerVoxels = outerVoxels;
			_voxelBones = voxelBones;
			_voxelColors = voxelColors;
			_compressedIndices = compressedIndices;
			_compressedBones = compressedBones;
			_compressedColors = compressedColors;
		}

		public void Execute(int index) {
			if (_outerVoxels[index]) return;
			_compressedIndices.Add(index);
			_compressedBones.Add(_voxelBones[index]);
			_compressedColors.Add(_voxelColors[index]);
		}
	}
}