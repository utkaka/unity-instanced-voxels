using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct SetupRuntimePlainVoxelsJob : IJobFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeSlice<VoxelPlain> _plainVoxels;
		[WriteOnly]
		private NativeList<ShaderVoxel> _voxels;
		[WriteOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxMasks;
		[WriteOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxBones;

		public SetupRuntimePlainVoxelsJob(VoxelsBox voxelsBox, NativeSlice<VoxelPlain> plainVoxels, NativeList<ShaderVoxel> voxels,
			NativeArray<byte> voxelBoxMasks, NativeArray<byte> voxelBoxBones) {
			_voxelsBox = voxelsBox;
			_plainVoxels = plainVoxels;
			_voxels = voxels;
			_voxelBoxMasks = voxelBoxMasks;
			_voxelBoxBones = voxelBoxBones;
		}

		public void Execute(int index) {
			var plainVoxel = _plainVoxels[index];
			_voxels.AddNoResize(new ShaderVoxel(plainVoxel.Position, new int3(1, 1, 1), plainVoxel.Bone, plainVoxel.Color));
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(plainVoxel.Position);
			_voxelBoxMasks[voxelIndex] = 1;
			_voxelBoxBones[voxelIndex] = (byte)plainVoxel.Bone;
		}
	}
}