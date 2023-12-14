using System;
using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedCube {
	public class InstancedCubesRenderer : MonoBehaviour, IVoxelRenderer{
		private static readonly int AnimationFrame = Shader.PropertyToID("_AnimationFrame");
		private static readonly int VoxelPositionsBuffer = Shader.PropertyToID("voxel_positions_buffer");
		private static readonly int VoxelBonesBuffer = Shader.PropertyToID("voxel_bones_buffer");
		private static readonly int VoxelColorsBuffer = Shader.PropertyToID("voxel_colors_buffer");
		private static readonly int BonePositionsBuffer = Shader.PropertyToID("bone_positions_buffer");
		private static readonly int BonePositionsAnimationBuffer = Shader.PropertyToID("bone_positions_animation_buffer");
		private static readonly int BoneRotationsAnimationBuffer = Shader.PropertyToID("bone_rotations_animation_buffer");
		private static readonly int BonesCount = Shader.PropertyToID("_BonesCount");
		private static readonly int AnimationFramesCount = Shader.PropertyToID("_AnimationFramesCount");

		[SerializeField]
		private Voxels _voxels;
		private Material _material;
		private Mesh _mesh;
		private int _positionsCount;
		private Bounds _bounds;
		private float _animationTime;
		private float _animationLength;
		
		private ComputeBuffer _positionBuffer;
		private ComputeBuffer _bonesBuffer;
		private ComputeBuffer _colorsBuffer;
		private ComputeBuffer _bonePositionsBuffer;
		private ComputeBuffer _bonePositionsAnimationBuffer;
		private ComputeBuffer _boneRotationsAnimationBuffer;


		public void Init(Voxels voxels) {
			_voxels = voxels;
		}

		private void Start() {
			InitVoxels();
		}
		
		private void InitVoxels() {
			_bounds = new Bounds(Vector3.zero, 
				new Vector3(_voxels.Box.Size.x, _voxels.Box.Size.y, _voxels.Box.Size.z) * _voxels.VoxelSize);
			
			_mesh = VoxelMeshGenerator.GetCubeMesh(_voxels.VoxelSize);
			_material = new Material(Shader.Find("Shader Graphs/InstancedVoxelShader"));

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

			_positionsCount = positionsSlice.Length;
			var positionsArrayFloat = new NativeArray<float3>(_positionsCount, Allocator.Persistent);
			var colorsArrayFloat = new NativeArray<float3>(_positionsCount, Allocator.Persistent);
			var bonesArrayInt = new NativeArray<uint>(_positionsCount, Allocator.Persistent);

			var voxelBoxMasks = new NativeArray<byte>(box.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var voxelBoxBones = new NativeArray<byte>(box.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var voxelBoxBoneMasks = new NativeArray<byte>(box.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			var setupVoxelsJob = new SetupRuntimeVoxelsJob(_voxels.VoxelSize, _voxels.StartPosition, box, positionsSlice, colorsSlice,
				bonesSlice, positionsArrayFloat, colorsArrayFloat, bonesArrayInt, voxelBoxMasks, voxelBoxBones);
			var maskSameBoneJob = new MaskSameBoneJob(box, positionsSlice, voxelBoxBones, voxelBoxBoneMasks);
			var maskVoxelSidesJob =
				new MaskVoxelSidesJob(box, positionsSlice, voxelBoxBoneMasks, voxelBoxMasks);

			var sliceSize = _positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var handle = setupVoxelsJob.Schedule(_positionsCount, sliceSize);
			handle = maskSameBoneJob.Schedule(_positionsCount, sliceSize, handle);
			handle = maskVoxelSidesJob.Schedule(_positionsCount, sliceSize, handle);
			
			handle.Complete();
			
			UpdateVoxels(true, positionsArrayFloat, bonesArrayInt, colorsArrayFloat);
			
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
			voxelBoxBoneMasks.Dispose();

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
				_material.SetFloat(AnimationFrame, _animationTime);
			}
		}

		private void Update() {
			if (_positionsCount == 0) return;
			Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material,
				_bounds, _positionsCount, null, ShadowCastingMode.Off, false);
		}

		private void UpdateBones(int animationFramesCount, NativeArray<float3> bonePositions, NativeArray<float3> boneAnimationPositions,
			NativeArray<float4> boneAnimationRotations) {
			_material.SetFloat(AnimationFramesCount, animationFramesCount);
			_material.SetFloat(BonesCount, bonePositions.Length);
			SetBufferData(BonePositionsBuffer, ref _bonePositionsBuffer, bonePositions, 12);
			SetBufferData(BonePositionsAnimationBuffer, ref _bonePositionsAnimationBuffer, boneAnimationPositions, 12);
			SetBufferData(BoneRotationsAnimationBuffer, ref _boneRotationsAnimationBuffer, boneAnimationRotations, 16);
		}
		
		private void UpdateVoxels(bool visible, NativeArray<float3> voxelPositions, NativeArray<uint> voxelBones, NativeArray<float3> voxelColors) {
			_positionsCount = !visible ? 0 : voxelPositions.Length;
			if (_positionsCount == 0) {
				_positionBuffer?.Dispose();
				_bonesBuffer?.Dispose();
				_colorsBuffer?.Dispose();
				return;
			}
			SetBufferData(VoxelPositionsBuffer, ref _positionBuffer, voxelPositions, 12);
			SetBufferData(VoxelBonesBuffer, ref _bonesBuffer, voxelBones, 4);
			SetBufferData(VoxelColorsBuffer, ref _colorsBuffer, voxelColors, 12);
		}

		private void SetBufferData<T>(int nameId, ref ComputeBuffer buffer, NativeArray<T> data, int stride) where T : struct {
			buffer?.Dispose();
			buffer = new ComputeBuffer(data.Length, stride);
			buffer.SetData(data);
			_material.SetBuffer(nameId, buffer);
		}

		private void OnDestroy() {
			_positionBuffer?.Dispose();
			_bonesBuffer?.Dispose();
			_colorsBuffer?.Dispose();
			_bonePositionsBuffer?.Dispose();
			_bonePositionsAnimationBuffer?.Dispose();
			_boneRotationsAnimationBuffer?.Dispose();
		}
	}
}