using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedCube;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad {
	public class QuadRenderer : IDisposable {
		private static readonly int VoxelPositionsBuffer = Shader.PropertyToID("voxel_positions_buffer");
		private static readonly int VoxelBonesBuffer = Shader.PropertyToID("voxel_bones_buffer");
		private static readonly int VoxelColorsBuffer = Shader.PropertyToID("voxel_colors_buffer");
		
		private readonly int _sideIndex;
		private readonly Material _material;
		private readonly Mesh _mesh;
		private readonly CullingOptions _cullingOptions;
		
		private int _positionsCount;
		
		private ComputeBuffer _positionBuffer;
		private ComputeBuffer _bonesBuffer;
		private ComputeBuffer _colorsBuffer;
		private ComputeBuffer _bonePositionsBuffer;
		private ComputeBuffer _bonePositionsAnimationBuffer;
		private ComputeBuffer _boneRotationsAnimationBuffer;


		public QuadRenderer(int sideIndex, float voxelSize, CullingOptions cullingOptions) {
			_sideIndex = sideIndex;
			_cullingOptions = cullingOptions;
			_mesh = VoxelMeshGenerator.GetSideMesh(sideIndex, voxelSize);
			_material = new Material(Shader.Find("Shader Graphs/InstancedVoxelShader"));
		}

		public void Render(Bounds bounds) {
			if (_positionsCount == 0) return;
			Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material,
				bounds, _positionsCount, null, ShadowCastingMode.Off, false);
		}

		public void InitVoxels(int positionsCount, VoxelsBox box, NativeSlice<byte3> positionsSlice,
			NativeArray<float3> positionsArrayFloat, NativeArray<float3> colorsArrayFloat,
			NativeArray<uint> bonesArrayInt, NativeArray<byte> voxelBoxMasks, JobHandle handle) {
			var positionsListFloat = new NativeList<float3>(positionsCount, Allocator.TempJob);
			var colorsListFloat = new NativeList<float3>(positionsCount, Allocator.TempJob);
			var bonesListInt = new NativeList<uint>(positionsCount, Allocator.TempJob);
			
			if (_cullingOptions == CullingOptions.InnerVoxels) {
				var cullInvisibleVoxelsJob = new CullInvisibleVoxelsJob(box, positionsSlice, voxelBoxMasks,
					positionsArrayFloat, colorsArrayFloat, bonesArrayInt, positionsListFloat, colorsListFloat,
					bonesListInt);
				handle = cullInvisibleVoxelsJob.Schedule(positionsCount, handle);
				handle.Complete();
				UpdateVoxels(true, positionsListFloat.AsArray(), bonesListInt.AsArray(), colorsListFloat.AsArray());
			} else if (_cullingOptions == CullingOptions.InnerSides) {
				var cullInvisibleSidesJob = new CullInvisibleSidesJob(box, _sideIndex, positionsSlice, voxelBoxMasks,
					positionsArrayFloat, colorsArrayFloat, bonesArrayInt, positionsListFloat, colorsListFloat,
					bonesListInt);
				handle = cullInvisibleSidesJob.Schedule(positionsCount, handle);
				handle.Complete();
				UpdateVoxels(true, positionsListFloat.AsArray(), bonesListInt.AsArray(), colorsListFloat.AsArray());
			} else {
				handle.Complete();
				UpdateVoxels(true, positionsArrayFloat, bonesArrayInt, colorsArrayFloat);
			}
			
			positionsListFloat.Dispose();
			bonesListInt.Dispose();
			colorsListFloat.Dispose();
		}
		
		private void UpdateVoxels(bool visible, NativeArray<float3> voxelPositions, NativeArray<uint> voxelBones, NativeArray<float3> voxelColors) {
			_positionsCount = !visible ? 0 : voxelPositions.Length;
			if (_positionsCount == 0) {
				_positionBuffer?.Dispose();
				_bonesBuffer?.Dispose();
				_colorsBuffer?.Dispose();
				_positionBuffer = null;
				_bonesBuffer = null;
				_colorsBuffer = null;
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

		public void Dispose() {
			_positionBuffer?.Dispose();
			_bonesBuffer?.Dispose();
			_colorsBuffer?.Dispose();
			_bonePositionsBuffer?.Dispose();
			_bonePositionsAnimationBuffer?.Dispose();
			_boneRotationsAnimationBuffer?.Dispose();
		}
	}
}