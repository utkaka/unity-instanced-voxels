using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader {
    public unsafe class BrgStandardShaderRenderer : BrgRenderer {
        private BatchMetadata _batchMetadata = new(new PerInstanceMetadataValue<float4x3>("unity_ObjectToWorld"),
            new PerInstanceMetadataValue<float4x3>("unity_WorldToObject"),
            new PerInstanceMetadataValue<float4>("_BaseColor"));

        protected override BatchMetadata BatchMetadata => _batchMetadata;

        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        }

        protected override JobHandle FillBuffer(int outerVoxelsCount, NativeArray<byte> buffer, JobHandle handle) {
            var bufferPointer = (byte*)buffer.GetUnsafePtr();
            var worldToObjectPointer = bufferPointer + BatchMetadata.GetValueOffset(1, outerVoxelsCount);
            var colorPointer = bufferPointer + BatchMetadata.GetValueOffset(2, outerVoxelsCount);
            
            var updatePositionsJob = new UpdatePositionsJob(_startPosition, _voxelSize, _outerVoxels, _shaderVoxelsArray, 
                (float4x3*)bufferPointer, (float4x3*)worldToObjectPointer, (float4*)colorPointer);
            return updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle);
        }
    }
}