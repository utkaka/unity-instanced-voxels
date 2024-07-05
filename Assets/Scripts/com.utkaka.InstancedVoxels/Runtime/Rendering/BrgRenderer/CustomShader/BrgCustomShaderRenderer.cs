using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    public class BrgCustomShaderRenderer : BrgRenderer {
        private static readonly int ShaderObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        private static readonly int ShaderWorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        private static readonly int ShaderPositionBoneID = Shader.PropertyToID("_PositionBone");
        private static readonly int ShaderColorID = Shader.PropertyToID("_Color");
        private static readonly int ShaderVoxelSize = Shader.PropertyToID("_VoxelSize");
        private static readonly int ShaderStartPosition = Shader.PropertyToID("_StartPosition");
        private static readonly int ShaderAnimationFramesCount = Shader.PropertyToID("_AnimationFramesCount");
        
        private NativeArray<float> _cpuGraphicsBuffer;

        protected override void InitVoxels() {
            base.InitVoxels();
            Shader.SetGlobalFloat(ShaderVoxelSize, _voxelSize);
            Shader.SetGlobalVector(ShaderStartPosition, _startPosition);
            Shader.SetGlobalFloat(ShaderAnimationFramesCount, _animationLength);
        }

        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Custom/BrgVoxelShader"));
        }
        
        protected override NativeArray<MetadataValue> CreateMetadata(int positionsCount) {
            var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            batchMetadata[0] = CreateMetadataValue(ShaderPositionBoneID, 0, true);       // positions with bone
            batchMetadata[1] = CreateMetadataValue(ShaderColorID, positionsCount * 4, true); // colors
            batchMetadata[2] = CreateMetadataValue(ShaderObjectToWorldID, positionsCount * 8, false);       // matrices
            batchMetadata[3] = CreateMetadataValue(ShaderWorldToObjectID, positionsCount * 8 + 3 * 16, false);
            return batchMetadata;
        }

        protected override void UpdateBuffer(int outerVoxelsCount, JobHandle handle) {
            if (_cpuGraphicsBuffer.IsCreated) {
                _cpuGraphicsBuffer.Dispose();
                _graphicsBuffer.Dispose();
            }
			
            _cpuGraphicsBuffer = new NativeArray<float>(3 * 4 * 2 + outerVoxelsCount * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var index = outerVoxelsCount * 2;
            _cpuGraphicsBuffer[index++] = 1.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 1.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 1.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 1.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 1.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 1.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index++] = 0.0f;
            _cpuGraphicsBuffer[index] = 0.0f;


            var updatePositionsJob = new UpdatePositionsJob(outerVoxelsCount, _outerVoxels, _shaderVoxelsArray, _cpuGraphicsBuffer);
            updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle).Complete();
			
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, _cpuGraphicsBuffer.Length, 4);
            _graphicsBuffer.SetData(_cpuGraphicsBuffer, 0, 0, _cpuGraphicsBuffer.Length);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            _cpuGraphicsBuffer.Dispose();
        }
    }
}