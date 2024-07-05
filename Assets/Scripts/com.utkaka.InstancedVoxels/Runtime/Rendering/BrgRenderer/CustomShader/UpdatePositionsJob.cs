using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    [BurstCompile]
    public struct UpdatePositionsJob : IJobParallelFor {

        private readonly int _colorOffset;
        [ReadOnly] private NativeList<int> _outerVoxelsIndices;
        [ReadOnly] private NativeArray<ShaderVoxel> _inputVoxels;

        [WriteOnly, NativeDisableParallelForRestriction]
        private NativeArray<float> _sysmemBuffer;

        public UpdatePositionsJob(int positionsCount,
            NativeList<int> outerVoxelsIndices, NativeArray<ShaderVoxel> inputVoxels, NativeArray<float> sysmemBuffer) {
            _colorOffset = positionsCount;
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _sysmemBuffer = sysmemBuffer;
        }

        public void Execute(int index) {
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[index]];
            _sysmemBuffer[index] = inputVoxel.PositionBone;
            _sysmemBuffer[_colorOffset + index] = inputVoxel.Color;
        }
    }
}