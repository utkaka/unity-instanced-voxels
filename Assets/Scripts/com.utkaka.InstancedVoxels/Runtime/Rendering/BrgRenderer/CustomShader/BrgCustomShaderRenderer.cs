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
            new PerMaterialMetadataValue<float4x3>("unity_ObjectToWorld"),
            new PerMaterialMetadataValue<float4x3>("unity_WorldToObject"));

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
            var objectToWorldPointer = (float4x3*)(bufferPointer + BatchMetadata.GetValueOffset(2, outerVoxelsCount));
            var worldToObjectPointer = (float4x3*)(bufferPointer + BatchMetadata.GetValueOffset(3, outerVoxelsCount));

            *objectToWorldPointer = new float4x3(
                1.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f
            );
            
            *worldToObjectPointer = new float4x3(
                1.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f
            );
            
            var updatePositionsJob = new UpdatePositionsJob(_outerVoxels, _shaderVoxelsArray, (float*)bufferPointer, (float*)colorPointer);
            return updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle);
            
            /*var index = outerVoxelsCount * 2;
            buffer[index++] = 1.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 1.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 1.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 1.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 1.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 1.0f;
            buffer[index++] = 0.0f;
            buffer[index++] = 0.0f;
            buffer[index] = 0.0f;


            var updatePositionsJob = new UpdatePositionsJob(outerVoxelsCount, _outerVoxels, _shaderVoxelsArray, buffer);
            updatePositionsJob.Schedule(outerVoxelsCount,
                outerVoxelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle).Complete();
			
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferSizeInFloat, 4);
            _graphicsBuffer.SetData(buffer, 0, 0, bufferSizeInFloat);*/
        }

        protected override void OnDestroy() {
            base.OnDestroy();
        }
    }
}