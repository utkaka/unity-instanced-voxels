using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct CullBackfaceJob : IJobFor {
		[ReadOnly]
		private NativeArray<int> _outerVoxelsIndices;
		[ReadOnly]
		private NativeArray<ShaderVoxel> _inputVoxels;
		[ReadOnly]
		private NativeSlice<VoxelsBounds> _visibilityBounds;
		
		[WriteOnly]
		private NativeList<ShaderVoxel> _outputVoxels;

		public CullBackfaceJob(NativeArray<int> outerVoxelsIndices, NativeArray<ShaderVoxel> inputVoxels,
			NativeSlice<VoxelsBounds> visibilityBounds, NativeList<ShaderVoxel> outputVoxels) {
			_outerVoxelsIndices = outerVoxelsIndices;
			_inputVoxels = inputVoxels;
			_visibilityBounds = visibilityBounds;
			_outputVoxels = outputVoxels;
		}

		public void Execute(int index) {
			index = _outerVoxelsIndices[index];
			var inputVoxel = _inputVoxels[index];
			var voxelIndices = inputVoxel.GetPosition();
			var bone = inputVoxel.GetBone();
			if (!_visibilityBounds[bone].Contains(new int3(voxelIndices.x, voxelIndices.y, voxelIndices.z))) return;
			_outputVoxels.AddNoResize(inputVoxel);
		}
	}
}