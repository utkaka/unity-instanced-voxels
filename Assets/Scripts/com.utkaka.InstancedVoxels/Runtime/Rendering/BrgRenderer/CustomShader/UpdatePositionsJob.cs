using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    [BurstCompile]
    public unsafe struct UpdatePositionsJob : IJobParallelFor {
        private readonly int _indexOffset;
        [ReadOnly] private NativeList<int> _outerVoxelsIndices;
        [ReadOnly] private NativeArray<ShaderVoxel> _inputVoxels;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float3* _positionBuffer;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float* _boneBuffer;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float* _colorBuffer;

        public UpdatePositionsJob(int indexOffset, NativeList<int> outerVoxelsIndices,
            NativeArray<ShaderVoxel> inputVoxels, float3* positionBuffer, float* boneBuffer, float* colorBuffer) {
            _indexOffset = indexOffset; 
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _positionBuffer = positionBuffer;
            _boneBuffer = boneBuffer;
            _colorBuffer = colorBuffer;
        }

        public void Execute(int index) {
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[index + _indexOffset]];
            _positionBuffer[index] = inputVoxel.Position;
            _boneBuffer[index] = inputVoxel.Bone;
            _colorBuffer[index] = inputVoxel.Color;
        }
    }
}