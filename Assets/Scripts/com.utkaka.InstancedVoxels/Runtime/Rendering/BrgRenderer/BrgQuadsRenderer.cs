using System;
using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public class BrgQuadsRenderer : MonoBehaviour, IVoxelRenderer {
		[SerializeField]
		private Voxels _voxels;
		[SerializeField]
		private CullingOptions _cullingOptions;
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


		public void Init(Voxels voxels, CullingOptions cullingOptions) {
			_voxels = voxels;
			_cullingOptions = cullingOptions;
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
			
			_quadRenderers = new BrgQuadRenderer[6];
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i] = new BrgQuadRenderer(i, _voxelSize, _cullingOptions, _material);
			}
			
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
			
			_outerVoxels = new NativeList<int>(_positionsCount, Allocator.Persistent);
			var cullInnerVoxelsJob = new CullInnerVoxelsJob(_box, positionsSlice, _voxelBoxMasks, _outerVoxels);
			handle = cullInnerVoxelsJob.Schedule(_positionsCount, handle);
			handle.Complete();
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].InitVoxels(_positionsCount, _box, _shaderVoxelsArray, _voxelBoxMasks,
					_outerVoxels, handle, _startPosition, _voxelSize);
			}

			positionsArray.Dispose();
			colorsArray.Dispose();
			bonesArray.Dispose();

			voxelBoxBones.Dispose();
			boneMasks.Dispose();

			if (!(_voxels.Animation.FrameRate > 0)) return;
			if (_animationLength > 0.0f) {
				StartCoroutine(AnimationUpdate());
			}

			_voxels = null;
		}

		private IEnumerator AnimationUpdate() {
			var wait = new WaitForEndOfFrame();
			while (true) {
				yield return wait;
				_animationTime += _animationFrameRate * Time.deltaTime;
				if (_animationTime >= _animationLength) {
					_animationTime -= _animationLength;
				}
				_animationCurrentFrame = (int)_animationTime;
				_animationNextFrame = (_animationCurrentFrame + 1) % _animationLength;
				_animationLerpRatio = _animationTime - _animationCurrentFrame;
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
		}
    }
}