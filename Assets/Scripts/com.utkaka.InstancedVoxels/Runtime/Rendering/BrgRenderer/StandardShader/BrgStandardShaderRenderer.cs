using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader {
    public class BrgStandardShaderRenderer : BrgRenderer {
        private const int InstanceSize = (3 + 3 + 1) * 16;
        
        private BatchMetadata _batchMetadata = new(new PerInstanceMetadataValue<float4x3>("unity_ObjectToWorld"),
            new PerInstanceMetadataValue<float4x3>("unity_WorldToObject"),
            new PerInstanceMetadataValue<float4>("_BaseColor"));

        protected override BatchMetadata BatchMetadata => _batchMetadata;
        
        private NativeArray<float4> _cpuGraphicsBuffer;
        
        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        }

        protected override void UpdateBuffer(int outerVoxelsCount, JobHandle handle) {
            if (_cpuGraphicsBuffer.IsCreated) {
                _cpuGraphicsBuffer.Dispose();
                _graphicsBuffer.Dispose();
            }
			
            _cpuGraphicsBuffer = new NativeArray<float4>(outerVoxelsCount * InstanceSize / 16, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var updatePositionsJob = new UpdatePositionsJob(_startPosition, _voxelSize, outerVoxelsCount, _outerVoxels, _shaderVoxelsArray, _cpuGraphicsBuffer);
            updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle).Complete();
			
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, outerVoxelsCount * InstanceSize / 4, 4);
            _graphicsBuffer.SetData(_cpuGraphicsBuffer, 0, 0, _cpuGraphicsBuffer.Length);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            _cpuGraphicsBuffer.Dispose();
        }
    }
}