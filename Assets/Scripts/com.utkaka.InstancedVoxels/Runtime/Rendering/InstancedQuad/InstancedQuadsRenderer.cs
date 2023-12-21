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
			
			var box = new VoxelsBox(_voxels.Box.Size + new int3(2, 2, 2));
			
			var colorsArray = new NativeArray<byte>(_voxels.Colors, Allocator.TempJob);
			var colorsSlice = new NativeSlice<byte>(colorsArray).SliceConvert<byte3>();
			var positionsArray = new NativeArray<byte>(_voxels.Indices, Allocator.TempJob);
			var positionsSlice = new NativeSlice<byte>(positionsArray).SliceConvert<byte3>();
			var bonesArray = new NativeArray<byte>(_voxels.Bones, Allocator.TempJob);
			var bonesSlice = new NativeSlice<byte>(bonesArray).SliceConvert<byte>();

			var bonePositionsArray = _voxels.Animation.BonesPositions.Length == 0
				? new NativeArray<float3>(1, Allocator.TempJob)
				: new NativeArray<float3>(_voxels.Animation.BonesPositions, Allocator.TempJob);
			var boneAnimationPositionsArray = _voxels.Animation.AnimationBonesPositions.Length == 0
				? new NativeArray<float3>(1, Allocator.TempJob)
				: new NativeArray<float3>(_voxels.Animation.AnimationBonesPositions, Allocator.TempJob);
			var boneAnimationRotationsArray = _voxels.Animation.AnimationBonesRotations.Length == 0
				? new NativeArray<float4>(1, Allocator.TempJob)
				: new NativeArray<float4>(_voxels.Animation.AnimationBonesRotations, Allocator.TempJob);

			UpdateBones(_voxels.Animation.FramesCount, bonePositionsArray, boneAnimationPositionsArray,
				boneAnimationRotationsArray);

			var positionsCount = positionsSlice.Length;
			var positionsArrayFloat = new NativeArray<float3>(positionsCount, Allocator.TempJob);
			var colorsArrayFloat = new NativeArray<float3>(positionsCount, Allocator.TempJob);
			var bonesArrayInt = new NativeArray<uint>(positionsCount, Allocator.TempJob);
			var boneMasks = new NativeArray<byte>(positionsCount, Allocator.TempJob);

			var voxelBoxMasks = new NativeArray<byte>(box.Count, Allocator.TempJob);
			var voxelBoxBones = new NativeArray<byte>(box.Count, Allocator.TempJob);

			var setupVoxelsJob = new SetupRuntimeVoxelsJob(_voxels.VoxelSize, _voxels.StartPosition, box, positionsSlice, colorsSlice,
				bonesSlice, positionsArrayFloat, colorsArrayFloat, bonesArrayInt, voxelBoxMasks, voxelBoxBones);
			var maskSameBoneJob = new MaskSameBoneJob(box, positionsSlice, voxelBoxBones, boneMasks);
			var maskVoxelSidesJob =
				new MaskVoxelSidesJob(box, positionsSlice, boneMasks, voxelBoxMasks);

			var sliceSize = positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var handle = setupVoxelsJob.Schedule(positionsCount, sliceSize);
			handle = maskSameBoneJob.Schedule(positionsCount, sliceSize, handle);
			handle = maskVoxelSidesJob.Schedule(positionsCount, sliceSize, handle);

			var bonesCount = bonePositionsArray.Length;
			var visibilityBounds = _cullingOptions == CullingOptions.InnerSidesAndBackface
				? new NativeArray<VoxelsBounds>(6 * bonesCount, Allocator.TempJob)
				: new NativeArray<VoxelsBounds>(0, Allocator.TempJob);
			
			if (_cullingOptions == CullingOptions.InnerSidesAndBackface) {
				var cameraPosition = Camera.main.transform.position;
				var cameraRotation = Quaternion.Inverse(Camera.main.transform.rotation);
				JobHandle visibilityBoundsHandle = default;
				var currentFrame = (int)_animationTime;
				var nextFrame = (int)((currentFrame + 1) % _animationLength);
				var frameTransitionRatio = _animationTime - currentFrame;
				for (var i = 0; i < 6; i++) {
					var calculateVisibilityBoundsJob =
						new CalculateVisibilityBoundsJob(_voxels.VoxelSize, _voxels.StartPosition, VoxelMeshGenerator.GetSideNormal(i), box, cameraPosition, cameraRotation,
							currentFrame, nextFrame, frameTransitionRatio,
							bonePositionsArray, boneAnimationPositionsArray, boneAnimationRotationsArray,
							new NativeSlice<VoxelsBounds>(visibilityBounds, bonesCount * i, bonesCount));
					visibilityBoundsHandle = JobHandle.CombineDependencies(visibilityBoundsHandle,
						calculateVisibilityBoundsJob.Schedule(bonesCount,
							bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle));
				}

				handle = visibilityBoundsHandle;
			}

			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].InitVoxels(positionsCount, box, positionsSlice, positionsArrayFloat, colorsArrayFloat,
					bonesArrayInt, voxelBoxMasks, visibilityBounds, handle);
			}
			
			colorsArray.Dispose();
			positionsArray.Dispose();
			bonesArray.Dispose();

			bonePositionsArray.Dispose();
			boneAnimationPositionsArray.Dispose();
			boneAnimationRotationsArray.Dispose();

			positionsArrayFloat.Dispose();
			colorsArrayFloat.Dispose();
			bonesArrayInt.Dispose();

			voxelBoxMasks.Dispose();
			voxelBoxBones.Dispose();
			boneMasks.Dispose();

			visibilityBounds.Dispose();

			if (!(_voxels.Animation.FrameRate > 0)) return;
			_animationLength = _voxels.Animation.FramesCount;
			if (_animationLength > 0.0f) {
				StartCoroutine(AnimationUpdate());
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

		private void Update() {
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Render(_bounds);
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
		}
	}
}