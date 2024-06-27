using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    [BurstCompile]
    public struct UpdatePositionsJob : IJobParallelFor {
        
        private readonly float3 _startPosition;
        private readonly float _voxelSize;
        private readonly int _worldToObjectOffset;
        private readonly int _colorOffset;
        [ReadOnly]
        private NativeList<ShaderVoxel> _inputVoxels;
		
        [WriteOnly, NativeDisableParallelForRestriction]
        private NativeArray<float4> _sysmemBuffer;

        public UpdatePositionsJob(float3 startPosition, float voxelSize, int positionsCount, NativeList<ShaderVoxel> inputVoxels, NativeArray<float4> sysmemBuffer) {
            _startPosition = startPosition;
            _voxelSize = voxelSize;
            _worldToObjectOffset = positionsCount * 3;
            _colorOffset = positionsCount * 6;
            _inputVoxels = inputVoxels;
            _sysmemBuffer = sysmemBuffer;
        }

        public void Execute(int index) {
            var inputVoxel = _inputVoxels[index];
            var instanceMatrixOffset = index * 3;

            // compute the new current frame matrix
            var voxelPosition = new float3((inputVoxel.PositionBone & 65280) >> 8,
                (inputVoxel.PositionBone & 16711680) >> 16, (inputVoxel.PositionBone & 4278190080) >> 24);
            voxelPosition = voxelPosition * _voxelSize + _startPosition;
            
            _sysmemBuffer[instanceMatrixOffset] = new float4(1, 0, 0, 0);
            _sysmemBuffer[instanceMatrixOffset + 1] = new float4(1.0f, 0, 0, 0);
            _sysmemBuffer[instanceMatrixOffset + 2] = new float4(1, voxelPosition.x, voxelPosition.y, voxelPosition.z);

            // compute the new inverse matrix (note: shortcut use identity because aligned cubes normals aren't affected by any non uniform scale
            _sysmemBuffer[_worldToObjectOffset + instanceMatrixOffset] = new float4(1, 0, 0, 0);
            _sysmemBuffer[_worldToObjectOffset + instanceMatrixOffset + 1] = new float4(1, 0, 0, 0);
            _sysmemBuffer[_worldToObjectOffset + instanceMatrixOffset + 2] = new float4(1, 0, 0, 0);

            // update colors
            var inputColor = new float4(inputVoxel.Color & 255, (inputVoxel.Color & 65280) >> 8,
                (inputVoxel.Color & 16711680) >> 16, 255.0f) / 255.0f;
            // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
            inputColor *= inputColor * (inputColor * 0.305306011f + 0.682171111f) + 0.012522878f;
            _sysmemBuffer[_colorOffset + index] = inputColor;
        }
    }
}