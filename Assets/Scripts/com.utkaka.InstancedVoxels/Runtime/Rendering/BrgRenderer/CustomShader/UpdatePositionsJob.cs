using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    [BurstCompile]
    public unsafe struct UpdatePositionsJob : IJobParallelFor {
        [ReadOnly] private NativeList<int> _outerVoxelsIndices;
        [ReadOnly] private NativeArray<ShaderVoxel> _inputVoxels;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float* _positionBoneBuffer;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float* _colorBuffer;

        public UpdatePositionsJob(NativeList<int> outerVoxelsIndices, NativeArray<ShaderVoxel> inputVoxels, float* positionBoneBuffer, float* colorBuffer) {
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _positionBoneBuffer = positionBoneBuffer;
            _colorBuffer = colorBuffer;
        }

        public void Execute(int index) {
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[index]];
            _positionBoneBuffer[index] = inputVoxel.PositionBone;
            _colorBuffer[index] = inputVoxel.Color;
        }
    }
}