using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct MaskVoxelSidesJob : IJobParallelFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeSlice<byte3> _inputPositions;
		[ReadOnly]
		private NativeArray<byte> _voxelBoxBoneMasks;
		[NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;

		public MaskVoxelSidesJob(VoxelsBox voxelsBox, NativeSlice<byte3> inputPositions, NativeArray<byte> voxelBoxBoneMasks, NativeArray<byte> voxelBoxMasks) {
			_voxelsBox = voxelsBox;
			_inputPositions = inputPositions;
			_voxelBoxBoneMasks = voxelBoxBoneMasks;
			_voxelBoxMasks = voxelBoxMasks;
		}

		public void Execute(int index) {
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(_inputPositions[index]);
			var mask = 1;
			var neighbourIndex = _voxelsBox.GetLeft(voxelIndex);
			mask |= ((_voxelBoxMasks[neighbourIndex] & 1) << 1) & _voxelBoxBoneMasks[neighbourIndex];
			neighbourIndex = _voxelsBox.GetRight(voxelIndex);
			mask |= ((_voxelBoxMasks[neighbourIndex] & 1) << 2) & _voxelBoxBoneMasks[neighbourIndex];
			neighbourIndex = _voxelsBox.GetBack(voxelIndex);
			mask |= ((_voxelBoxMasks[neighbourIndex] & 1) << 3) & _voxelBoxBoneMasks[neighbourIndex];
			neighbourIndex = _voxelsBox.GetFront(voxelIndex);
			mask |= ((_voxelBoxMasks[neighbourIndex] & 1) << 4) & _voxelBoxBoneMasks[neighbourIndex];
			neighbourIndex = _voxelsBox.GetBottom(voxelIndex);
			mask |= ((_voxelBoxMasks[neighbourIndex] & 1) << 5) & _voxelBoxBoneMasks[neighbourIndex];
			neighbourIndex = _voxelsBox.GetTop(voxelIndex);
			mask |= ((_voxelBoxMasks[neighbourIndex] & 1) << 6) & _voxelBoxBoneMasks[neighbourIndex];
			_voxelBoxMasks[voxelIndex] = (byte)mask;
		}
	}
}