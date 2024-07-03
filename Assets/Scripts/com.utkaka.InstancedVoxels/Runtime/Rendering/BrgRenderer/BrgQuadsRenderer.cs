using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public class BrgQuadsRenderer : MonoBehaviour, IVoxelRenderer {
	    private const int InstanceSize = (3 + 3 + 1) * 16;
	    
		[SerializeField]
		private Voxels _voxels;
		[SerializeField]
		private Material _material;
		
		private Bounds _bounds;

		private float _voxelSize;
		private Vector3 _startPosition;
		
		private VoxelsBox _box;
		private int _positionsCount;
		private int _bonesCount;
		
		private float _animationFrameRate;
		private float _animationTime;
		private int _animationLength;
		
		private int _animationCurrentFrame;
		private int _animationNextFrame;
		private float _animationLerpRatio;

		private BrgQuadRenderer[] _quadRenderers;
		
		private NativeArray<float3> _bonePositionsArray;
		private NativeArray<float3> _boneAnimationPositionsArray;
		private NativeArray<float4> _boneAnimationRotationsArray;
		
		private ComputeBuffer _bonePositionsBuffer;
		private ComputeBuffer _bonePositionsAnimationBuffer;
		private ComputeBuffer _boneRotationsAnimationBuffer;
		
		private NativeArray<ShaderVoxel> _shaderVoxelsArray;
		
		private NativeArray<byte> _voxelBoxMasks;
		private NativeList<int> _outerVoxels;
		
		private GraphicsBuffer _graphicsBuffer;
		private NativeArray<float4> _cpuGraphicsBuffer;


		public void Init(Voxels voxels, CullingOptions cullingOptions) {
			_voxels = voxels;
			if (_material == null) {
				_material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
			}
		}
		
		private void Start() {
			InitVoxels();
		}

		private void InitVoxels() {
			_voxelSize = _voxels.VoxelSize;
			_startPosition = _voxels.StartPosition;
			_bounds = new Bounds(Vector3.zero, 
				new Vector3(_voxels.Box.Size.x, _voxels.Box.Size.y, _voxels.Box.Size.z) * _voxels.VoxelSize);
			
			_box = new VoxelsBox(_voxels.Box.Size + new int3(2, 2, 2));
			
			_animationFrameRate = _voxels.Animation.FrameRate;
			_animationLength = _voxels.Animation.FramesCount;
			
			var colorsArray = new NativeArray<byte>(_voxels.Colors, Allocator.TempJob);
			var colorsSlice = new NativeSlice<byte>(colorsArray).SliceConvert<byte3>();
			var positionsArray = new NativeArray<byte>(_voxels.Indices, Allocator.TempJob);
			var positionsSlice = new NativeSlice<byte>(positionsArray).SliceConvert<byte3>();
			var bonesArray = new NativeArray<byte>(_voxels.Bones, Allocator.TempJob);
			var bonesSlice = new NativeSlice<byte>(bonesArray).SliceConvert<byte>();

			_bonePositionsArray = _voxels.Animation.BonesPositions.Length == 0
				? new NativeArray<float3>(2, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.BonesPositions, Allocator.Persistent);
			_boneAnimationPositionsArray = _voxels.Animation.AnimationBonesPositions.Length == 0
				? new NativeArray<float3>(2, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.AnimationBonesPositions, Allocator.Persistent);
			_boneAnimationRotationsArray = _voxels.Animation.AnimationBonesRotations.Length == 0
				? new NativeArray<float4>(2, Allocator.Persistent)
				: new NativeArray<float4>(_voxels.Animation.AnimationBonesRotations, Allocator.Persistent);

			_positionsCount = positionsSlice.Length;
			_shaderVoxelsArray = new NativeArray<ShaderVoxel>(_positionsCount, Allocator.Persistent);
			var boneMasks = new NativeArray<byte>(_positionsCount, Allocator.TempJob);

			_voxelBoxMasks = new NativeArray<byte>(_box.Count, Allocator.Persistent);
			var voxelBoxBones = new NativeArray<byte>(_box.Count, Allocator.TempJob);

			var setupVoxelsJob = new SetupRuntimeVoxelsJob(_box, positionsSlice, colorsSlice, bonesSlice,
				_shaderVoxelsArray, _voxelBoxMasks, voxelBoxBones);
			var maskSameBoneJob = new MaskSameBoneJob(_box, positionsSlice, voxelBoxBones, boneMasks);
			var maskVoxelSidesJob = new MaskVoxelSidesJob(_box, positionsSlice, boneMasks, _voxelBoxMasks);

			var sliceSize = _positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var handle = setupVoxelsJob.Schedule(_positionsCount, sliceSize);
			handle = maskSameBoneJob.Schedule(_positionsCount, sliceSize, handle);
			handle = maskVoxelSidesJob.Schedule(_positionsCount, sliceSize, handle);

			_bonesCount = _bonePositionsArray.Length;
			
			_quadRenderers = new BrgQuadRenderer[6];
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i] = new BrgQuadRenderer(i, _voxelSize, _startPosition, _bonesCount, _animationLength,
					_material, _box, _shaderVoxelsArray, _voxelBoxMasks, _bonePositionsArray,
					_boneAnimationPositionsArray, _boneAnimationRotationsArray);
			}
			
			UpdateOuterVoxels(handle);
			
			positionsArray.Dispose();
			colorsArray.Dispose();
			bonesArray.Dispose();

			voxelBoxBones.Dispose();
			boneMasks.Dispose();
			
			_voxels = null;
		}

		private void UpdateOuterVoxels(JobHandle handle) {
			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_outerVoxels = new NativeList<int>(_positionsCount, Allocator.Persistent);
			var cullInnerVoxelsJob = new CullInnerVoxelsJob(_box, _shaderVoxelsArray, _voxelBoxMasks, _outerVoxels);
			handle = cullInnerVoxelsJob.Schedule(_positionsCount, handle);
			handle.Complete();
			
			var outerVoxelsCount = _outerVoxels.Length;

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
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].UpdateOuterVoxels(outerVoxelsCount, _outerVoxels, _graphicsBuffer);
			}
		}

		private void OnDestroy() {
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Dispose();
			}

			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_bonePositionsBuffer?.Dispose();
			_bonePositionsAnimationBuffer?.Dispose();
			_boneRotationsAnimationBuffer?.Dispose();
			
			_bonePositionsArray.Dispose();
			_boneAnimationPositionsArray.Dispose();
			_boneAnimationRotationsArray.Dispose();
			
			_shaderVoxelsArray.Dispose();

			_voxelBoxMasks.Dispose();
			
			_graphicsBuffer.Dispose();
			_cpuGraphicsBuffer.Dispose();
		}
    }
}