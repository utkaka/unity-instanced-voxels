using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    public unsafe class BrgCustomShaderRenderer : BrgRenderer {
        private static readonly int ShaderVoxelSize = Shader.PropertyToID("_VoxelSize");
        private static readonly int ShaderStartPosition = Shader.PropertyToID("_StartPosition");
        private static readonly int ShaderAnimationFramesCount = Shader.PropertyToID("_AnimationFramesCount");

        protected override void InitVoxels() {
            BatchMetadata = new BatchMetadata(
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
                )),
                new PerInstanceMetadataValue<int3>("_Position"),
                new PerInstanceMetadataValue<int3>("_Size"),
                new PerInstanceMetadataValue<int>("_Bone"),
                new PerInstanceMetadataValue<int>("_Color"));
            base.InitVoxels();
            Shader.SetGlobalFloat(ShaderVoxelSize, _voxelSize);
            Shader.SetGlobalVector(ShaderStartPosition, _startPosition);
            Shader.SetGlobalFloat(ShaderAnimationFramesCount, _animationLength);
        }

        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Custom/BrgVoxelShader"));
        }

        protected override JobHandle FillBuffer(int outerVoxelsCount, int indexOffset, byte* buffer, JobHandle handle) {
            var positionPointer = buffer + BatchMetadata.GetValueOffset(2, outerVoxelsCount);
            var sizePointer = buffer + BatchMetadata.GetValueOffset(3, outerVoxelsCount);
            var bonePointer = buffer + BatchMetadata.GetValueOffset(4, outerVoxelsCount);
            var colorPointer = buffer + BatchMetadata.GetValueOffset(5, outerVoxelsCount);
            var updatePositionsJob = new UpdatePositionsJob(indexOffset, _outerVoxels, _shaderVoxelsArray,
                (float3*)positionPointer, (float3*)sizePointer, (float*)bonePointer, (float*)colorPointer);
            return updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle);
        }
    }
}