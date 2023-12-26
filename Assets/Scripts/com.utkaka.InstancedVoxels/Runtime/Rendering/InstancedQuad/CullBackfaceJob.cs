using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad {
	[BurstCompile]
	public struct CullBackfaceJob : IJobFor {
		[ReadOnly]
		private NativeArray<int> _outerVoxelsIndices;
		[ReadOnly]
		private NativeSlice<byte3> _inputIndices;
		
		[ReadOnly]
		private NativeArray<float3> _inputPositions;
		[ReadOnly]
		private NativeArray<float3> _inputColors;
		[ReadOnly]
		private NativeArray<uint> _inputBones;
		[ReadOnly]
		private NativeSlice<VoxelsBounds> _visibilityBounds;
		
		[WriteOnly]
		private NativeList<float3> _positions;
		[WriteOnly]
		private NativeList<float3> _colors;
		[WriteOnly]
		private NativeList<uint> _bones;

		public CullBackfaceJob(NativeArray<int> outerVoxelsIndices, NativeSlice<byte3> inputIndices, NativeArray<float3> inputPositions, NativeArray<float3> inputColors,
			NativeArray<uint> inputBones, NativeList<float3> positions, NativeList<float3> colors,
			NativeList<uint> bones, NativeSlice<VoxelsBounds> visibilityBounds) {
			_outerVoxelsIndices = outerVoxelsIndices;
			_inputIndices = inputIndices;
			_inputPositions = inputPositions;
			_inputColors = inputColors;
			_inputBones = inputBones;
			_positions = positions;
			_colors = colors;
			_bones = bones;
			_visibilityBounds = visibilityBounds;
		}

		public void Execute(int index) {
			index = _outerVoxelsIndices[index];
			var voxelIndices = _inputIndices[index];
			var bone = _inputBones[index];
			if (!_visibilityBounds[(int)bone].Contains(new int3(voxelIndices.x, voxelIndices.y, voxelIndices.z))) return;
			//var position = _inputPositions[index];
			//var color = _inputColors[index];
			_positions.AddNoResize(_inputPositions[index]);
			_colors.AddNoResize(_inputColors[index]);
			_bones.AddNoResize(bone);
			//_colors.AddNoResize(color);
		}
	}
}