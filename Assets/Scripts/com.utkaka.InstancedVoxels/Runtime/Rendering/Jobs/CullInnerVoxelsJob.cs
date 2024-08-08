using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
			var compressedVoxel = _voxels[index];
			for (var i = 0; i < compressedVoxel.Size.x; i++) {
				for (var j = 0; j < compressedVoxel.Size.y; j++) {
					for (var k = 0; k < compressedVoxel.Size.z; k++) {
						var position = compressedVoxel.Position1 + new int3(i, j, k);
						var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(position);
						if (_voxelBoxMasks[voxelIndex] >= 127) continue;
						_outerVoxelsIndices.AddNoResize(index);
						return;
					}
				}
			}
		}
	}
}