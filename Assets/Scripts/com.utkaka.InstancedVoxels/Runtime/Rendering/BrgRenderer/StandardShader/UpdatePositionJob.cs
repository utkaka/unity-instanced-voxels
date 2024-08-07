using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader
{
    [BurstCompile]
    public unsafe struct UpdatePositionsJob : IJobParallelFor {
        private readonly int _indexOffset;
        private readonly float3 _startPosition;
        private readonly float _voxelSize;
        [ReadOnly]
        private NativeList<int> _outerVoxelsIndices;
        [ReadOnly]
        private NativeArray<ShaderVoxel> _inputVoxels;
		
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float4x3* _objectToWorldPointer;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly float4* _colorPointer;


        public UpdatePositionsJob(int indexOffset, float3 startPosition, float voxelSize, NativeList<int> outerVoxelsIndices, NativeArray<ShaderVoxel> inputVoxels, float4x3* objectToWorldPointer, float4* colorPointer) {
            _indexOffset = indexOffset;
            _startPosition = startPosition;
            _voxelSize = voxelSize;
            _outerVoxelsIndices = outerVoxelsIndices;
            _inputVoxels = inputVoxels;
            _objectToWorldPointer = objectToWorldPointer;
            _colorPointer = colorPointer;
        }

        public void Execute(int index) {
            var inputVoxel = _inputVoxels[_outerVoxelsIndices[index + _indexOffset]];

            // compute the new current frame matrix
            var voxelPosition = (float3)inputVoxel.Position;
            voxelPosition = voxelPosition * _voxelSize + _startPosition;

            _objectToWorldPointer[index] = new float4x3(
                1.0f, 1.0f, 1.0f,
                0.0f, 0.0f, voxelPosition.x,
                0.0f, 0.0f, voxelPosition.y,
                0.0f, 0.0f, voxelPosition.z
            );

            // update colors
            var inputColor = new float4(inputVoxel.Color & 255, (inputVoxel.Color & 65280) >> 8,
                (inputVoxel.Color & 16711680) >> 16, 255.0f) / 255.0f;
            // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
            inputColor *= inputColor * (inputColor * 0.305306011f + 0.682171111f) + 0.012522878f;
            _colorPointer[index] = inputColor;
        }
    }
}