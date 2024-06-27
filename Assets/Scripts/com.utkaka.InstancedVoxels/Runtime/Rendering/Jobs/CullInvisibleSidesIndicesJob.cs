using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct CullInvisibleSidesIndicesJob : IJobFor {
		private readonly int _sideMask;
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeArray<int> _outerIndices;
		[ReadOnly]
		private NativeArray<ShaderVoxel> _inputVoxels;
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		
		[WriteOnly]
		private NativeList<int> _visibleVoxelsIndices;

		public CullInvisibleSidesIndicesJob(int sideIndex, VoxelsBox voxelsBox, NativeArray<int> outerIndices,
			NativeArray<ShaderVoxel> inputVoxels, NativeArray<byte> voxelBoxMasks,
			NativeList<int> visibleVoxelsIndices) {
			_sideMask = 1 << (sideIndex + 1);
			_voxelsBox = voxelsBox;
			_outerIndices = outerIndices;
			_inputVoxels = inputVoxels;
			_voxelBoxMasks = voxelBoxMasks;
			_visibleVoxelsIndices = visibleVoxelsIndices;
		}

		public void Execute(int index) {
			index = _outerIndices[index];
			var voxelIndex = _inputVoxels[index].GetPosition();
			if ((_voxelBoxMasks[_voxelsBox.GetExtendedVoxelIndex(voxelIndex)] & _sideMask) == _sideMask) return;
			_visibleVoxelsIndices.AddNoResize(index);
		}
	}
}