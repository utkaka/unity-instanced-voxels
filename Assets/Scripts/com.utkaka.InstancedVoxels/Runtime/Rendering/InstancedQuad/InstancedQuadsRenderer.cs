using System;
using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedCube;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad {
	public class InstancedQuadsRenderer : MonoBehaviour, IVoxelRenderer{
		private static readonly int AnimationFrame = Shader.PropertyToID("_AnimationFrame");
		private static readonly int BonePositionsBuffer = Shader.PropertyToID("bone_positions_buffer");
		private static readonly int BonePositionsAnimationBuffer = Shader.PropertyToID("bone_positions_animation_buffer");
		private static readonly int BoneRotationsAnimationBuffer = Shader.PropertyToID("bone_rotations_animation_buffer");
		private static readonly int BonesCount = Shader.PropertyToID("_BonesCount");
		private static readonly int AnimationFramesCount = Shader.PropertyToID("_AnimationFramesCount");

		[SerializeField]
		private Voxels _voxels;
		[SerializeField]
		private CullingOptions _cullingOptions;
		private Bounds _bounds;
		private float _animationTime;
		private float _animationLength;

		private QuadRenderer[] _quadRenderers;
		
		private ComputeBuffer _bonePositionsBuffer;
		private ComputeBuffer _bonePositionsAnimationBuffer;
		private ComputeBuffer _boneRotationsAnimationBuffer;
		
		private VoxelsBox _box;
		private int _positionsCount;
		private int _bonesCount;
		
		private NativeArray<VoxelsBounds> _visibilityBounds;
		private NativeArray<float3> _bonePositionsArray;
		private NativeArray<float3> _boneAnimationPositionsArray;
		private NativeArray<float4> _boneAnimationRotationsArray;
		private NativeArray<byte> _positionsArray;
		private NativeSlice<byte3> _positionsSlice;
		private NativeArray<float3> _positionsArrayFloat;
		private NativeArray<float3> _colorsArrayFloat;
		private NativeArray<uint> _bonesArrayInt;
		private NativeArray<byte> _voxelBoxMasks;


		public void Init(Voxels voxels, CullingOptions cullingOptions) {
			_voxels = voxels;
			_cullingOptions = cullingOptions;
		}

		private void Start() {
			InitVoxels();
		}
		
		private void InitVoxels() {
			_quadRenderers = new QuadRenderer[6];
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i] = new QuadRenderer(i, _voxels.VoxelSize, _cullingOptions);
			}
			
			_bounds = new Bounds(Vector3.zero, 
				new Vector3(_voxels.Box.Size.x, _voxels.Box.Size.y, _voxels.Box.Size.z) * _voxels.VoxelSize);
			
			_box = new VoxelsBox(_voxels.Box.Size + new int3(2, 2, 2));
			
			var colorsArray = new NativeArray<byte>(_voxels.Colors, Allocator.TempJob);
			var colorsSlice = new NativeSlice<byte>(colorsArray).SliceConvert<byte3>();
			_positionsArray = new NativeArray<byte>(_voxels.Indices, Allocator.Persistent);
			_positionsSlice = new NativeSlice<byte>(_positionsArray).SliceConvert<byte3>();
			var bonesArray = new NativeArray<byte>(_voxels.Bones, Allocator.TempJob);
			var bonesSlice = new NativeSlice<byte>(bonesArray).SliceConvert<byte>();

			_bonePositionsArray = _voxels.Animation.BonesPositions.Length == 0
				? new NativeArray<float3>(1, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.BonesPositions, Allocator.Persistent);
			_boneAnimationPositionsArray = _voxels.Animation.AnimationBonesPositions.Length == 0
				? new NativeArray<float3>(1, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.AnimationBonesPositions, Allocator.Persistent);
			_boneAnimationRotationsArray = _voxels.Animation.AnimationBonesRotations.Length == 0
				? new NativeArray<float4>(1, Allocator.Persistent)
				: new NativeArray<float4>(_voxels.Animation.AnimationBonesRotations, Allocator.Persistent);

			UpdateBones(_voxels.Animation.FramesCount, _bonePositionsArray, _boneAnimationPositionsArray,
				_boneAnimationRotationsArray);

			_positionsCount = _positionsSlice.Length;
			_positionsArrayFloat = new NativeArray<float3>(_positionsCount, Allocator.Persistent);
			_colorsArrayFloat = new NativeArray<float3>(_positionsCount, Allocator.Persistent);
			_bonesArrayInt = new NativeArray<uint>(_positionsCount, Allocator.Persistent);
			var boneMasks = new NativeArray<byte>(_positionsCount, Allocator.TempJob);

			_voxelBoxMasks = new NativeArray<byte>(_box.Count, Allocator.Persistent);
			var voxelBoxBones = new NativeArray<byte>(_box.Count, Allocator.TempJob);

			var setupVoxelsJob = new SetupRuntimeVoxelsJob(_voxels.VoxelSize, _voxels.StartPosition, _box, _positionsSlice, colorsSlice,
				bonesSlice, _positionsArrayFloat, _colorsArrayFloat, _bonesArrayInt, _voxelBoxMasks, voxelBoxBones);
			var maskSameBoneJob = new MaskSameBoneJob(_box, _positionsSlice, voxelBoxBones, boneMasks);
			var maskVoxelSidesJob =
				new MaskVoxelSidesJob(_box, _positionsSlice, boneMasks, _voxelBoxMasks);

			var sliceSize = _positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var handle = setupVoxelsJob.Schedule(_positionsCount, sliceSize);
			handle = maskSameBoneJob.Schedule(_positionsCount, sliceSize, handle);
			handle = maskVoxelSidesJob.Schedule(_positionsCount, sliceSize, handle);

			_bonesCount = _bonePositionsArray.Length;
			_visibilityBounds = _cullingOptions == CullingOptions.InnerSidesAndBackface || _cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate
				? new NativeArray<VoxelsBounds>(6 * _bonesCount, Allocator.Persistent)
				: new NativeArray<VoxelsBounds>(0, Allocator.Persistent);
			
			if (_cullingOptions == CullingOptions.InnerSidesAndBackface || _cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate) {
				var cameraPosition = Camera.main.transform.position;
				var cameraForward = Camera.main.transform.forward;
				JobHandle visibilityBoundsHandle = default;
				var currentFrame = (int)_animationTime;
				var nextFrame = (int)((currentFrame + 1) % _animationLength);
				var frameTransitionRatio = _animationTime - currentFrame;
				for (var i = 0; i < 6; i++) {
					var calculateVisibilityBoundsJob =
						new CalculateVisibilityBoundsJob(_voxels.VoxelSize, _voxels.StartPosition, VoxelMeshGenerator.GetSideNormal(i), _box, cameraPosition, cameraForward,
							_voxels.Animation.FramesCount, currentFrame, nextFrame, frameTransitionRatio,
							_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
							new NativeSlice<VoxelsBounds>(_visibilityBounds, _bonesCount * i, _bonesCount));
					visibilityBoundsHandle = JobHandle.CombineDependencies(visibilityBoundsHandle,
						calculateVisibilityBoundsJob.Schedule(_bonesCount,
							_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle));
				}

				handle = visibilityBoundsHandle;
			}

			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].InitVoxels(_positionsCount, _box, _positionsSlice, _positionsArrayFloat, _colorsArrayFloat,
					_bonesArrayInt, _voxelBoxMasks, _visibilityBounds, handle);
			}
			
			colorsArray.Dispose();
			bonesArray.Dispose();
			

			voxelBoxBones.Dispose();
			boneMasks.Dispose();

			if (!(_voxels.Animation.FrameRate > 0)) return;
			_animationLength = _voxels.Animation.FramesCount;
			if (_animationLength > 0.0f) {
				StartCoroutine(AnimationUpdate());
			}
		}

		private void LateUpdate() {
			if (_cullingOptions != CullingOptions.InnerSidesAndBackfaceUpdate) return;
			var cameraPosition = Camera.main.transform.position;
			var cameraForward = Camera.main.transform.forward;
			var currentFrame = (int)_animationTime;
			var nextFrame = (int)((currentFrame + 1) % _animationLength);
			var frameTransitionRatio = _animationTime - currentFrame;
			for (var i = 0; i < 6; i++) {
				var calculateVisibilityBoundsJob =
					new CalculateVisibilityBoundsJob(_voxels.VoxelSize, _voxels.StartPosition, VoxelMeshGenerator.GetSideNormal(i), _box, cameraPosition, cameraForward,
						_voxels.Animation.FramesCount, currentFrame, nextFrame, frameTransitionRatio,
						_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
						new NativeSlice<VoxelsBounds>(_visibilityBounds, _bonesCount * i, _bonesCount));
				var visibilityBoundsHandle = calculateVisibilityBoundsJob.Schedule(_bonesCount,
					_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount);
				_quadRenderers[i].CullingUpdate(_positionsCount, _box, _positionsSlice, _positionsArrayFloat, _colorsArrayFloat,
					_bonesArrayInt, _voxelBoxMasks, _visibilityBounds, visibilityBoundsHandle);
			}
		}
		
		private void Update() {
			if (_cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate) {
				for (var i = 0; i < 6; i++) {
					_quadRenderers[i].UpdateVoxels();
				}
			}
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Render(_bounds);
			}
		}

		private IEnumerator AnimationUpdate() {
			var wait = new WaitForEndOfFrame();
			while (true) {
				yield return wait;
				_animationTime += _voxels.Animation.FrameRate * Time.deltaTime;
				if (_animationTime >= _animationLength) {
					_animationTime -= _animationLength;
				}
				Shader.SetGlobalFloat(AnimationFrame, _animationTime);
			}
		}

		private void UpdateBones(int animationFramesCount, NativeArray<float3> bonePositions, NativeArray<float3> boneAnimationPositions,
			NativeArray<float4> boneAnimationRotations) {
			Shader.SetGlobalFloat(AnimationFramesCount, animationFramesCount);
			Shader.SetGlobalFloat(BonesCount, bonePositions.Length);
			SetGlobalBufferData(BonePositionsBuffer, ref _bonePositionsBuffer, bonePositions, 12);
			SetGlobalBufferData(BonePositionsAnimationBuffer, ref _bonePositionsAnimationBuffer, boneAnimationPositions, 12);
			SetGlobalBufferData(BoneRotationsAnimationBuffer, ref _boneRotationsAnimationBuffer, boneAnimationRotations, 16);
		}

		private void SetGlobalBufferData<T>(int nameId, ref ComputeBuffer buffer, NativeArray<T> data, int stride) where T : struct {
			buffer?.Dispose();
			buffer = new ComputeBuffer(data.Length, stride);
			buffer.SetData(data);
			Shader.SetGlobalBuffer(nameId, buffer);
		}

		private void OnDestroy() {
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Dispose();
			}
			_bonePositionsBuffer?.Dispose();
			_bonePositionsAnimationBuffer?.Dispose();
			_boneRotationsAnimationBuffer?.Dispose();
			
			_bonePositionsArray.Dispose();
			_boneAnimationPositionsArray.Dispose();
			_boneAnimationRotationsArray.Dispose();
			
			_visibilityBounds.Dispose();
			_positionsArray.Dispose();
			
			_positionsArrayFloat.Dispose();
			_colorsArrayFloat.Dispose();
			_bonesArrayInt.Dispose();

			_voxelBoxMasks.Dispose();
		}
	}
}