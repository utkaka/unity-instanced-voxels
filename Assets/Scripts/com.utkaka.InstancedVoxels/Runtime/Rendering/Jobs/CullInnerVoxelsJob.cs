using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct CullInnerVoxelsJob : IJobFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeArray<ShaderVoxel> _voxels;
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		
		[WriteOnly]
		private NativeList<int> _outerVoxelsIndices;

		public CullInnerVoxelsJob(VoxelsBox voxelsBox, NativeArray<ShaderVoxel> voxels, NativeArray<byte> voxelBoxMasks,
			NativeList<int> outerVoxelsIndices) {
			_voxelsBox = voxelsBox;
			_voxels = voxels;
			_voxelBoxMasks = voxelBoxMasks;
			_outerVoxelsIndices = outerVoxelsIndices;
		}

		public void Execute(int index) {
			var voxelPosition = _voxels[index].GetPosition();
			if (_voxelBoxMasks[_voxelsBox.GetExtendedVoxelIndex(voxelPosition)] >= 127) return;
			_outerVoxelsIndices.AddNoResize(index);
		}
	}
}