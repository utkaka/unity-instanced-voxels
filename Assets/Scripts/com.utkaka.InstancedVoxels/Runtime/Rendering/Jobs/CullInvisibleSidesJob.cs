using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	public struct CullInvisibleSidesJob : IJobFor {
		private readonly VoxelsBox _voxelsBox;
		private readonly int _sideMask;
		[ReadOnly]
		private NativeArray<ShaderVoxel> _inputVoxels;
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		
		[WriteOnly]
		private NativeList<ShaderVoxel> _outputVoxels;

		public CullInvisibleSidesJob(VoxelsBox voxelsBox, int sideMask, NativeArray<ShaderVoxel> inputVoxels,
			NativeArray<byte> voxelBoxMasks, NativeList<ShaderVoxel> outputVoxels) {
			_voxelsBox = voxelsBox;
			_sideMask = 1 << (sideMask + 1);
			_inputVoxels = inputVoxels;
			_voxelBoxMasks = voxelBoxMasks;
			_outputVoxels = outputVoxels;
		}

		public void Execute(int index) {
			var inputVoxel = _inputVoxels[index];
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(inputVoxel.GetPosition());
			if ((_voxelBoxMasks[voxelIndex] & _sideMask) == _sideMask) return;
			_outputVoxels.AddNoResize(inputVoxel);
		}
	}
}