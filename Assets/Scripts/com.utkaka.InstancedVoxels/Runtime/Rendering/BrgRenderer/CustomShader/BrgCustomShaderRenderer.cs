using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.CustomShader {
    public class BrgCustomShaderRenderer : BrgRenderer {
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

        protected override void CreateQuadRenderers() {
            for (var i = 0; i < 6; i++) {
                _quadRenderers[i] = new BrgCustomShaderQuadRenderer(i, _voxelSize, _startPosition, _bonesCount, _animationLength,
                    _material, _box, _shaderVoxelsArray, _voxelBoxMasks, _bonePositionsArray,
                    _boneAnimationPositionsArray, _boneAnimationRotationsArray);
            }
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