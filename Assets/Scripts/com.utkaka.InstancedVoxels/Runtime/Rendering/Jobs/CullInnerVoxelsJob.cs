using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct CullInnerVoxelsJob : IJobFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeSlice<byte3> _inputIndices;
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		
		[WriteOnly]
		private NativeList<int> _outerVoxelsIndices;

		public CullInnerVoxelsJob(VoxelsBox voxelsBox, NativeSlice<byte3> inputIndices, NativeArray<byte> voxelBoxMasks,
			NativeList<int> outerVoxelsIndices) {
			_voxelsBox = voxelsBox;
			_inputIndices = inputIndices;
			_voxelBoxMasks = voxelBoxMasks;
			_outerVoxelsIndices = outerVoxelsIndices;
		}

		public void Execute(int index) {
			var voxelIndex = _inputIndices[index];
			if (_voxelBoxMasks[_voxelsBox.GetExtendedVoxelIndex(voxelIndex)] >= 127) return;
			_outerVoxelsIndices.AddNoResize(index);
		}
	}
}