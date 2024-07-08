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
        private readonly int _sideIndex;
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
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<int> _visibleSideVoxelsCount;
        [WriteOnly]
        private NativeArray<int> _previousVisibleIndices;

        private readonly int _offset;

        public FillVisibleInstancesJob(int sideIndex, NativeArray<int> sideVoxelsIndices, NativeArray<int> outerVoxelsIndices,
            NativeArray<ShaderVoxel> inputVoxels,
            NativeSlice<VoxelsBounds> visibilityBounds, int* outputVoxelIndices, int offset,
            NativeArray<int> visibleSideVoxelsCount, NativeArray<int> previousVisibleIndices) {
            _sideIndex = sideIndex;
            _sideVoxelsIndices = sideVoxelsIndices;
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _visibilityBounds = visibilityBounds;
            _outputVoxelIndices = outputVoxelIndices;
            _offset = offset;
            _visibleSideVoxelsCount = visibleSideVoxelsCount;
            _previousVisibleIndices = previousVisibleIndices;
        }

        public void Execute(int index) {
            var sideVoxelIndex = _sideVoxelsIndices[index];
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[sideVoxelIndex]];
            var voxelIndices = inputVoxel.GetPosition();
            var bone = inputVoxel.GetBone();
            if (!_visibilityBounds[bone].Contains(new int3(voxelIndices.x, voxelIndices.y, voxelIndices.z))) return;
            var visibleCount = _visibleSideVoxelsCount[_sideIndex];
            _previousVisibleIndices[visibleCount] = sideVoxelIndex;
            _outputVoxelIndices[_offset + visibleCount] = sideVoxelIndex;
            visibleCount++;
            _visibleSideVoxelsCount[_sideIndex] = visibleCount;
        }
    }
}