using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer {
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
            var compressedVoxel = _inputVoxels[_outerIndices[index]];
            for (var i = 0; i < compressedVoxel.Size.x; i++) {
                for (var j = 0; j < compressedVoxel.Size.y; j++) {
                    for (var k = 0; k < compressedVoxel.Size.z; k++) {
                        var position = compressedVoxel.Position1 + new int3(i, j, k);
                        var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(position);
                        if ((_voxelBoxMasks[voxelIndex] & _sideMask) == _sideMask) continue;
                        _visibleVoxelsIndices.AddNoResize(index);
                        return;
                    }
                }
            }
        }
    }
}