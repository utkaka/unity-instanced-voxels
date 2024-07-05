using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader {
    public class BrgStandardShaderRenderer : BrgRenderer{
        private static readonly int ShaderObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        private static readonly int  ShaderWorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        private static readonly int  ShaderColorID = Shader.PropertyToID("_BaseColor");
        
        private const int InstanceSize = (3 + 3 + 1) * 16;
        
        private NativeArray<float4> _cpuGraphicsBuffer;
        
        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        }
        
        protected override NativeArray<MetadataValue> CreateMetadata(int positionsCount) {
            var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            batchMetadata[0] = CreateMetadataValue(ShaderObjectToWorldID, 0, true);       // matrices
            batchMetadata[1] = CreateMetadataValue(ShaderWorldToObjectID, positionsCount * 3 * 16, true); // inverse matrices
            batchMetadata[2] = CreateMetadataValue(ShaderColorID, positionsCount * 3 * 2 * 16, true); // colors
            return batchMetadata;
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