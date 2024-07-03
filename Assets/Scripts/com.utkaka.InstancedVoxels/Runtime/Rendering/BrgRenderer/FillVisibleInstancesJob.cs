using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    [BurstCompile]
    public unsafe struct FillVisibleInstancesJob : IJobFor {
        [ReadOnly]
        private NativeArray<int> _sideVoxelsIndices;
        [ReadOnly]
        private NativeArray<int> _outerVoxelsIndices;
        [ReadOnly]
        private NativeArray<ShaderVoxel> _inputVoxels;
        [ReadOnly]
        private NativeSlice<VoxelsBounds> _visibilityBounds;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly int* _outputVoxelIndices;
        private NativeArray<int> _visibleSideVoxelsCount;

        public FillVisibleInstancesJob(NativeArray<int> sideVoxelsIndices, NativeArray<int> outerVoxelsIndices,
            NativeArray<ShaderVoxel> inputVoxels,
            NativeSlice<VoxelsBounds> visibilityBounds, int* outputVoxelIndices,
            NativeArray<int> visibleSideVoxelsCount) {
            _sideVoxelsIndices = sideVoxelsIndices;
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _visibilityBounds = visibilityBounds;
            _outputVoxelIndices = outputVoxelIndices;
            _visibleSideVoxelsCount = visibleSideVoxelsCount;
        }

        public void Execute(int index) {
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[_sideVoxelsIndices[index]]];
            var voxelIndices = inputVoxel.GetPosition();
            var bone = inputVoxel.GetBone();
            if (!_visibilityBounds[bone].Contains(new int3(voxelIndices.x, voxelIndices.y, voxelIndices.z))) return;
            _outputVoxelIndices[_visibleSideVoxelsCount[0]++] = _sideVoxelsIndices[index];
        }
    }
}