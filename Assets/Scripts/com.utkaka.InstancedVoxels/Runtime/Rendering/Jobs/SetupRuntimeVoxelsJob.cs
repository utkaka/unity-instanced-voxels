using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct SetupRuntimeVoxelsJob : IJobParallelFor {
		private readonly float _voxelSize;
		private readonly float3 _startPosition;
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeSlice<byte3> _inputPositions;
		[ReadOnly]
		private NativeSlice<byte3> _inputColors;
		[ReadOnly]
		private NativeSlice<byte> _inputBones;
		[WriteOnly]
		private NativeArray<float3> _positions;
		[WriteOnly]
		private NativeArray<float3> _colors;
		[WriteOnly]
		private NativeArray<uint> _bones;
		[WriteOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		[WriteOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxBones;

		public SetupRuntimeVoxelsJob(float voxelSize, float3 startPosition, VoxelsBox voxelsBox, NativeSlice<byte3> inputPositions,
			NativeSlice<byte3> inputColors, NativeSlice<byte> inputBones, NativeArray<float3> positions,
			NativeArray<float3> colors, NativeArray<uint> bones, NativeArray<byte> voxelBoxMasks,
			NativeArray<byte> voxelBoxBones) {
			_voxelSize = voxelSize;
			_startPosition = startPosition;
			_voxelsBox = voxelsBox;
			_inputPositions = inputPositions;
			_inputColors = inputColors;
			_inputBones = inputBones;
			_positions = positions;
			_colors = colors;
			_bones = bones;
			_voxelBoxMasks = voxelBoxMasks;
			_voxelBoxBones = voxelBoxBones;
		}

		public void Execute(int index) {
			var bytePosition = _inputPositions[index];
			_positions[index] = new float3(bytePosition.x, bytePosition.y, bytePosition.z) * _voxelSize +_startPosition;
			_bones[index] = _inputBones[index];
			_colors[index] = ColorUtils.GammaToLinearSpace(_inputColors[index]);
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(bytePosition);
			_voxelBoxMasks[voxelIndex] = 1;
			_voxelBoxBones[voxelIndex] = _inputBones[index];
		}
	}
}