using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader {
    public unsafe class BrgStandardShaderRenderer : BrgRenderer {
        protected override void InitVoxels() {
            BatchMetadata = new BatchMetadata(new PerInstanceMetadataValue<float4x3>("unity_ObjectToWorld"),
                new PerMaterialMetadataValue<float4x3>("unity_WorldToObject", new float4x3(
                    1.0f, 1.0f, 1.0f,
                    0.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, 0.0f
                )),
                new PerInstanceMetadataValue<float4>("_BaseColor"));
            base.InitVoxels();
        }

        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        }

        protected override JobHandle FillBuffer(int outerVoxelsCount, int indexOffset, byte* buffer, JobHandle handle) {
            var colorPointer = buffer + BatchMetadata.GetValueOffset(2, outerVoxelsCount);
            
            var updatePositionsJob = new UpdatePositionsJob(indexOffset, _startPosition, _voxelSize, _outerVoxels, _shaderVoxelsArray, 
                (float4x3*)buffer, (float4*)colorPointer);
            return updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle);
        }
    }
}