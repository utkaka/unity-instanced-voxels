using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct SetupRuntimeVoxelsJob : IJobParallelFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeSlice<byte3> _inputPositions;
		[ReadOnly]
		private NativeSlice<byte3> _inputColors;
		[ReadOnly]
		private NativeSlice<byte> _inputBones;
		[WriteOnly]
		private NativeArray<ShaderVoxel> _voxels;
		[WriteOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		[WriteOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxBones;

		public SetupRuntimeVoxelsJob(VoxelsBox voxelsBox, NativeSlice<byte3> inputPositions,
			NativeSlice<byte3> inputColors, NativeSlice<byte> inputBones, NativeArray<ShaderVoxel> voxels,
			NativeArray<byte> voxelBoxMasks, NativeArray<byte> voxelBoxBones) {
			_voxelsBox = voxelsBox;
			_inputPositions = inputPositions;
			_inputColors = inputColors;
			_inputBones = inputBones;
			_voxels = voxels;
			_voxelBoxMasks = voxelBoxMasks;
			_voxelBoxBones = voxelBoxBones;
		}

		public void Execute(int index) {
			var bytePosition = _inputPositions[index];
			var bone = _inputBones[index];
			_voxels[index] = new ShaderVoxel(bytePosition, bone, _inputColors[index]);
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(bytePosition);
			_voxelBoxMasks[voxelIndex] = 1;
			_voxelBoxBones[voxelIndex] = bone;
		}
	}
}