using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
    [BurstCompile]
    public struct SetupRuntimeCompressedVoxelsJob : IJobFor {
        private readonly VoxelsBox _voxelsBox;
        [ReadOnly]
        private NativeSlice<VoxelCombined> _compressedVoxels;
        [WriteOnly]
        private NativeList<ShaderVoxel> _voxels;
        [WriteOnly, NativeDisableParallelForRestriction]
        private NativeArray<byte> _voxelBoxMasks;
        [WriteOnly, NativeDisableParallelForRestriction]
        private NativeArray<byte> _voxelBoxBones;

        public SetupRuntimeCompressedVoxelsJob(VoxelsBox voxelsBox, NativeSlice<VoxelCombined> compressedVoxels,
            NativeList<ShaderVoxel> voxels,
            NativeArray<byte> voxelBoxMasks, NativeArray<byte> voxelBoxBones) {
            _voxelsBox = voxelsBox;
            _compressedVoxels = compressedVoxels;
            _voxels = voxels;
            _voxelBoxMasks = voxelBoxMasks;
            _voxelBoxBones = voxelBoxBones;
        }

        public void Execute(int index) {
            var compressedVoxel = _compressedVoxels[index];
            for (var i = 0; i < compressedVoxel.Size.x; i++) {
                for (var j = 0; j < compressedVoxel.Size.y; j++) {
                    for (var k = 0; k < compressedVoxel.Size.z; k++) {
                        var position = compressedVoxel.Position + new int3(i, j, k);
                        _voxels.AddNoResize(new ShaderVoxel(position, compressedVoxel.Bone, compressedVoxel.Color));
                        var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(position);
                        _voxelBoxMasks[voxelIndex] = 1;
                        _voxelBoxBones[voxelIndex] = (byte)compressedVoxel.Bone;
                    }
                }
            }
        }
    }
}