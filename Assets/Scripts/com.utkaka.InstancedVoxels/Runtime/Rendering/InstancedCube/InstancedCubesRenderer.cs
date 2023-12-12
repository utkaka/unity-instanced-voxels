using System;
using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
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
		private NativeArray<float3> _positionsArrayFloat;
		private NativeArray<float3> _colorsArrayFloat;
		private NativeArray<uint> _bonesArrayInt;


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
			var colorsArray = new NativeArray<byte>(_voxels.Colors, Allocator.Temp);
			var colorsSlice = new NativeSlice<byte>(colorsArray).SliceConvert<byte3>();
			var positionsArray = new NativeArray<byte>(_voxels.Indices, Allocator.Temp);
			var positionsSlice = new NativeSlice<byte>(positionsArray).SliceConvert<byte3>();
			var bonesArray = new NativeArray<byte>(_voxels.Bones, Allocator.Temp);
			var bonesSlice = new NativeSlice<byte>(bonesArray).SliceConvert<byte>();
			var voxelSize = _voxels.VoxelSize;
			var startPosition = (float3)_voxels.StartPosition;

			var bonePositionsArray = _voxels.Animation.BonesPositions.Length == 0
				? new NativeArray<float3>(1, Allocator.Temp)
				: new NativeArray<float3>(_voxels.Animation.BonesPositions, Allocator.Temp);
			var boneAnimationPositionsArray = _voxels.Animation.AnimationBonesPositions.Length == 0
				? new NativeArray<float3>(1, Allocator.Temp)
				: new NativeArray<float3>(_voxels.Animation.AnimationBonesPositions, Allocator.Temp);
			var boneAnimationRotationsArray = _voxels.Animation.AnimationBonesRotations.Length == 0
				? new NativeArray<float4>(1, Allocator.Temp)
				: new NativeArray<float4>(_voxels.Animation.AnimationBonesRotations, Allocator.Temp);

			UpdateBones(_voxels.Animation.FramesCount, bonePositionsArray, boneAnimationPositionsArray,
				boneAnimationRotationsArray);

			_positionsCount = positionsSlice.Length;
			_positionsArrayFloat = new NativeArray<float3>(_positionsCount, Allocator.Persistent);
			_colorsArrayFloat = new NativeArray<float3>(_positionsCount, Allocator.Persistent);
			_bonesArrayInt = new NativeArray<uint>(_positionsCount, Allocator.Persistent);
			
			
			for (var i = 0; i < _positionsCount; i++) {
				var bytePosition = positionsSlice[i];
				var position = new float3(bytePosition.x, bytePosition.y, bytePosition.z);
				var color = colorsSlice[i];
				_positionsArrayFloat[i] = startPosition + position * voxelSize;
				_bonesArrayInt[i] = bonesSlice[i];
				_colorsArrayFloat[i] = new float3(Mathf.GammaToLinearSpace(color.x / 255.0f), Mathf.GammaToLinearSpace(color.y / 255.0f), Mathf.GammaToLinearSpace(color.z / 255.0f));
			}
			
			UpdateVoxels(true, _positionsArrayFloat, _bonesArrayInt, _colorsArrayFloat);
			
			colorsArray.Dispose();
			positionsArray.Dispose();
			bonesArray.Dispose();

			bonePositionsArray.Dispose();
			boneAnimationPositionsArray.Dispose();
			boneAnimationRotationsArray.Dispose();

			_positionsArrayFloat.Dispose();
			_colorsArrayFloat.Dispose();
			_bonesArrayInt.Dispose();

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