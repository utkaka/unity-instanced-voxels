using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad {
	[BurstCompile]
	public struct CullInvisibleSidesAndBackfaceJob : IJobFor {
		private readonly VoxelsBox _voxelsBox;
		private readonly int _sideMask;
		[ReadOnly]
		private NativeSlice<byte3> _inputIndices;
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		
		[ReadOnly]
		private NativeArray<float3> _inputPositions;
		[ReadOnly]
		private NativeArray<float3> _inputColors;
		[ReadOnly]
		private NativeArray<uint> _inputBones;
		[ReadOnly]
		private NativeSlice<VoxelsBounds> _visibilityBounds;
		
		[WriteOnly]
		private NativeList<float3>.ParallelWriter _positions;
		[WriteOnly]
		private NativeList<float3>.ParallelWriter _colors;
		[WriteOnly]
		private NativeList<uint>.ParallelWriter _bones;

		public CullInvisibleSidesAndBackfaceJob(VoxelsBox voxelsBox, int sideIndex, NativeSlice<byte3> inputIndices,
			NativeArray<byte> voxelBoxMasks, NativeArray<float3> inputPositions, NativeArray<float3> inputColors,
			NativeArray<uint> inputBones, NativeList<float3>.ParallelWriter positions, NativeList<float3>.ParallelWriter colors,
			NativeList<uint>.ParallelWriter bones, NativeSlice<VoxelsBounds> visibilityBounds) {
			_voxelsBox = voxelsBox;
			_sideMask = 1 << (sideIndex + 1);
			_inputIndices = inputIndices;
			_voxelBoxMasks = voxelBoxMasks;
			_inputPositions = inputPositions;
			_inputColors = inputColors;
			_inputBones = inputBones;
			_positions = positions;
			_colors = colors;
			_bones = bones;
			_visibilityBounds = visibilityBounds;
		}

		public void Execute(int index) {
			var voxelIndices = _inputIndices[index];
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(voxelIndices);
			if ((_voxelBoxMasks[voxelIndex] & _sideMask) == _sideMask) return;
			var bone = _inputBones[index];
			if (!_visibilityBounds[(int)bone].Contains(new int3(voxelIndices.x, voxelIndices.y, voxelIndices.z))) return;
			_positions.AddNoResize(_inputPositions[index]);
			_colors.AddNoResize(_inputColors[index]);
			_bones.AddNoResize(bone);
		}
	}
}