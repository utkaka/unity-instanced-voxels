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
        private readonly int _instancesPerBatch;
        private readonly int _batchesCount;
        private int _batchIndex;
        private int _currentCount;
        private int _totalCount;
        [ReadOnly]
        private NativeArray<int> _sideVoxelsIndices;
        [ReadOnly]
        private NativeArray<int> _outerVoxelsIndices;
        [ReadOnly]
        private NativeArray<ShaderVoxel> _inputVoxels;
        [ReadOnly]
        private NativeSlice<VoxelsBounds> _visibilityBounds;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private int* _outputVoxelIndices;
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<int> _visibleSideVoxelsOffsets;
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<int> _visibleSideVoxelsCount;
        [WriteOnly]
        private NativeArray<int> _previousVisibleIndices;

        public FillVisibleInstancesJob(int sideIndex, int instancesPerBatch, int batchesCount, NativeArray<int> sideVoxelsIndices, NativeArray<int> outerVoxelsIndices,
            NativeArray<ShaderVoxel> inputVoxels,
            NativeSlice<VoxelsBounds> visibilityBounds, int* outputVoxelIndices,
            NativeArray<int> visibleSideVoxelsCount, NativeArray<int> previousVisibleIndices, NativeArray<int> visibleSideVoxelsOffsets) {
            _sideIndex = sideIndex;
            _instancesPerBatch = instancesPerBatch;
            _batchesCount = batchesCount;
            _sideVoxelsIndices = sideVoxelsIndices;
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _visibilityBounds = visibilityBounds;
            _outputVoxelIndices = outputVoxelIndices;
            _visibleSideVoxelsCount = visibleSideVoxelsCount;
            _previousVisibleIndices = previousVisibleIndices;
            _batchIndex = 0;
            _currentCount = 0;
            _totalCount = 0;
            _visibleSideVoxelsOffsets = visibleSideVoxelsOffsets;
        }

        public void Execute(int index) {
            var sideVoxelIndex = _sideVoxelsIndices[index];
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[sideVoxelIndex]];
            var bone = inputVoxel.Bone;
            for (var i = 0; i < inputVoxel.Size.x; i++) {
                for (var j = 0; j < inputVoxel.Size.y; j++) {
                    for (var k = 0; k < inputVoxel.Size.z; k++) {
                        var position = inputVoxel.Position1 + new int3(i, j, k);
                        if (!_visibilityBounds[bone].Contains(position)) continue;
                        while (sideVoxelIndex >= (_batchIndex + 1) * _instancesPerBatch) {
                            _outputVoxelIndices += _currentCount;
                            _batchIndex++;
                            _visibleSideVoxelsOffsets[_sideIndex * _batchesCount + _batchIndex] =
                                _visibleSideVoxelsOffsets[_sideIndex * _batchesCount + _batchIndex - 1] + _currentCount;
                            _currentCount = 0;
                        }
                        _previousVisibleIndices[_totalCount] = sideVoxelIndex - _batchIndex * _instancesPerBatch;
                        _outputVoxelIndices[_currentCount] = sideVoxelIndex - _batchIndex * _instancesPerBatch;
                        _currentCount++;
                        _totalCount++;
                        _visibleSideVoxelsCount[_sideIndex * _batchesCount + _batchIndex] = _currentCount;
                        return;
                    }
                }
            }
        }
    }
}