using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.StandardShader {
    public class BrgStandardShaderRenderer : BrgRenderer{
        private const int InstanceSize = (3 + 3 + 1) * 16;
        
        private NativeArray<float4> _cpuGraphicsBuffer;
        
        protected override Material GetDefaultMaterial() {
            return new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        }

        protected override void CreateQuadRenderers() {
            for (var i = 0; i < 6; i++) {
                _quadRenderers[i] = new BrgStandardShaderQuadRenderer(i, _voxelSize, _startPosition, _bonesCount, _animationLength,
                    _material, _box, _shaderVoxelsArray, _voxelBoxMasks, _bonePositionsArray,
                    _boneAnimationPositionsArray, _boneAnimationRotationsArray);
            }
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