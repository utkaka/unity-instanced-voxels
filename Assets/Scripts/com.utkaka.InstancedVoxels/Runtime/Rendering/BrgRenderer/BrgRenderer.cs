using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public abstract class BrgRenderer : MonoBehaviour, IVoxelRenderer {
		[SerializeField]
		protected Voxels _voxels;
		[SerializeField]
		protected Material _material;
		
		protected Bounds _bounds;

		protected float _voxelSize;
		protected Vector3 _startPosition;
		protected VoxelsBox _box;
		protected int _positionsCount;
		protected int _bonesCount;
		
		protected float _animationFrameRate;
		protected float _animationTime;
		protected int _animationLength;
		
		protected int _animationCurrentFrame;
		protected int _animationNextFrame;
		protected float _animationLerpRatio;
		
		protected BrgQuadRenderer[] _quadRenderers;
		
		protected NativeArray<ShaderVoxel> _shaderVoxelsArray;
		protected NativeArray<byte> _voxelBoxMasks;
		protected NativeList<int> _outerVoxels;
		
		protected NativeArray<float3> _bonePositionsArray;
		protected NativeArray<float3> _boneAnimationPositionsArray;
		protected NativeArray<float4> _boneAnimationRotationsArray;
		
		protected GraphicsBuffer _graphicsBuffer;

		public void Init(Voxels voxels, CullingOptions cullingOptions) {
			_voxels = voxels;
			if (_material == null) {
				_material = GetDefaultMaterial();
			}
		}
		
		private void Start() {
			InitVoxels();
		}

		protected abstract Material GetDefaultMaterial();

		protected virtual void InitVoxels() {
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
			CreateQuadRenderers();
			
			UpdateOuterVoxels(handle);
			
			positionsArray.Dispose();
			colorsArray.Dispose();
			bonesArray.Dispose();

			voxelBoxBones.Dispose();
			boneMasks.Dispose();
			
			_voxels = null;
		}

		protected abstract void CreateQuadRenderers();

		private void UpdateOuterVoxels(JobHandle handle) {
			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_outerVoxels = new NativeList<int>(_positionsCount, Allocator.Persistent);
			var cullInnerVoxelsJob = new CullInnerVoxelsJob(_box, _shaderVoxelsArray, _voxelBoxMasks, _outerVoxels);
			handle = cullInnerVoxelsJob.Schedule(_positionsCount, handle);
			handle.Complete();
			
			var outerVoxelsCount = _outerVoxels.Length;
			
			UpdateBuffer(outerVoxelsCount, handle);
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].UpdateOuterVoxels(outerVoxelsCount, _outerVoxels, _graphicsBuffer);
			}
		}

		protected abstract void UpdateBuffer(int outerVoxelsCount, JobHandle handle);

		protected virtual void OnDestroy() {
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Dispose();
			}

			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_bonePositionsArray.Dispose();
			_boneAnimationPositionsArray.Dispose();
			_boneAnimationRotationsArray.Dispose();
			
			_shaderVoxelsArray.Dispose();

			_voxelBoxMasks.Dispose();
			
			_graphicsBuffer.Dispose();
		}
    }
}