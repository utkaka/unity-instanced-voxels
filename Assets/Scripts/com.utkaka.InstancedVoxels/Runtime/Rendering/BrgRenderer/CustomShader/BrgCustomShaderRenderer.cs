using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    public unsafe class BrgCustomShaderRenderer : BrgRenderer {
        private static readonly int ShaderVoxelSize = Shader.PropertyToID("_VoxelSize");
        private static readonly int ShaderStartPosition = Shader.PropertyToID("_StartPosition");
        private static readonly int ShaderAnimationFramesCount = Shader.PropertyToID("_AnimationFramesCount");
        
        private BatchMetadata _batchMetadata = new(new PerInstanceMetadataValue<int>("_PositionBone"),
            new PerInstanceMetadataValue<int>("_Color"),
            new PerMaterialMetadataValue<float4x3>("unity_ObjectToWorld", new float4x3(
                1.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f
            )),
            new PerMaterialMetadataValue<float4x3>("unity_WorldToObject", new float4x3(
                1.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f
            )));

        protected override BatchMetadata BatchMetadata => _batchMetadata;

        protected override void InitVoxels() {
            base.InitVoxels();
            Shader.SetGlobalFloat(ShaderVoxelSize, _voxelSize);
            Shader.SetGlobalVector(ShaderStartPosition, _startPosition);
            Shader.SetGlobalFloat(ShaderAnimationFramesCount, _animationLength);
        }

        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Custom/BrgVoxelShader"));
        }

        protected override JobHandle FillBuffer(int outerVoxelsCount, NativeArray<byte> buffer, JobHandle handle) {
            var bufferPointer = (byte*)buffer.GetUnsafePtr();
            var colorPointer = bufferPointer + BatchMetadata.GetValueOffset(1, outerVoxelsCount);
            var updatePositionsJob = new UpdatePositionsJob(_outerVoxels, _shaderVoxelsArray, (float*)bufferPointer, (float*)colorPointer);
            return updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle);
        }
    }
}