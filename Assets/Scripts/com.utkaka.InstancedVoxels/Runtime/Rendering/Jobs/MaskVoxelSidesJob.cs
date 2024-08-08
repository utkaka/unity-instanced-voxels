using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct MaskVoxelSidesJob : IJobParallelFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeList<ShaderVoxel> _shaderVoxels;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<byte> _boneMasks;
		[NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;

		public MaskVoxelSidesJob(VoxelsBox voxelsBox, NativeList<ShaderVoxel> shaderVoxels, NativeArray<byte> boneMasks, NativeArray<byte> voxelBoxMasks) {
			_voxelsBox = voxelsBox;
			_shaderVoxels = shaderVoxels;
			_boneMasks = boneMasks;
			_voxelBoxMasks = voxelBoxMasks;
		}

		public void Execute(int index) {
			var compressedVoxel = _shaderVoxels[index];
			for (var i = 0; i < compressedVoxel.Size.x; i++) {
				for (var j = 0; j < compressedVoxel.Size.y; j++) {
					for (var k = 0; k < compressedVoxel.Size.z; k++) {
						var mask = 1;
						var position = compressedVoxel.Position1 + new int3(i, j, k);
						var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(position);
						
						var neighbourIndex = _voxelsBox.GetLeft(voxelIndex);
						mask |= (_voxelBoxMasks[neighbourIndex] & 1) << 1;
						neighbourIndex = _voxelsBox.GetRight(voxelIndex);
						mask |= (_voxelBoxMasks[neighbourIndex] & 1) << 2;
						neighbourIndex = _voxelsBox.GetBack(voxelIndex);
						mask |= (_voxelBoxMasks[neighbourIndex] & 1) << 3;
						neighbourIndex = _voxelsBox.GetFront(voxelIndex);
						mask |= (_voxelBoxMasks[neighbourIndex] & 1) << 4;
						neighbourIndex = _voxelsBox.GetBottom(voxelIndex);
						mask |= (_voxelBoxMasks[neighbourIndex] & 1) << 5;
						neighbourIndex = _voxelsBox.GetTop(voxelIndex);
						mask |= (_voxelBoxMasks[neighbourIndex] & 1) << 6;
						mask &= _boneMasks[voxelIndex];
						_voxelBoxMasks[voxelIndex] = (byte)mask;
					}
				}
			}
		}
	}
}