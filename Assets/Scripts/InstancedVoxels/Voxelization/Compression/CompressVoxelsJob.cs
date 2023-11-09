using InstancedVoxels.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace InstancedVoxels.Voxelization.Compression {
	[BurstCompile]
	public struct CompressVoxelsJob : IJobFor {
		private VoxelsBox _box;
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		[ReadOnly]
		private NativeArray<int> _voxelBones;
		[ReadOnly]
		private NativeArray<byte3> _voxelColors;
		[WriteOnly]
		private NativeList<byte3> _compressedPositions;
		[WriteOnly]
		private NativeList<byte> _compressedBones;
		[WriteOnly]
		private NativeList<byte3> _compressedColors;

		public CompressVoxelsJob(VoxelsBox box, NativeArray<bool> outerVoxels, NativeArray<int> voxelBones,
			NativeArray<byte3> voxelColors, NativeList<byte3> compressedPositions, NativeList<byte> compressedBones,
			NativeList<byte3> compressedColors) : this() {
			_box = box;
			_outerVoxels = outerVoxels;
			_voxelBones = voxelBones;
			_voxelColors = voxelColors;
			_compressedPositions = compressedPositions;
			_compressedBones = compressedBones;
			_compressedColors = compressedColors;
		}

		public void Execute(int index) {
			if (_outerVoxels[index]) return;
			var position = _box.GetVoxelPosition(index);
			_compressedPositions.Add(new byte3((byte)position.x, (byte)position.y, (byte)position.z));
			_compressedBones.Add((byte)_voxelBones[index]);
			_compressedColors.Add(_voxelColors[index]);
		}
	}
}