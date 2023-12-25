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

		private JobHandle _cullingHandle;
		private ComputeBuffer _positionBuffer;
		private ComputeBuffer _bonesBuffer;
		private ComputeBuffer _colorsBuffer;
		private ComputeBuffer _bonePositionsBuffer;
		private ComputeBuffer _bonePositionsAnimationBuffer;
		private ComputeBuffer _boneRotationsAnimationBuffer;
		private NativeList<float3> _positionsListFloat;
		private NativeList<float3> _colorsListFloat;
		private NativeList<uint> _bonesListInt;


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
			NativeArray<uint> bonesArrayInt, NativeArray<byte> voxelBoxMasks,
			NativeArray<VoxelsBounds> visibilityBounds, JobHandle handle) {
			_positionsListFloat = new NativeList<float3>(positionsCount, Allocator.Persistent);
			_colorsListFloat = new NativeList<float3>(positionsCount, Allocator.Persistent);
			_bonesListInt = new NativeList<uint>(positionsCount, Allocator.Persistent);
			
			if (_cullingOptions == CullingOptions.InnerVoxels) {
				var cullInvisibleVoxelsJob = new CullInvisibleVoxelsJob(box, positionsSlice, voxelBoxMasks,
					positionsArrayFloat, colorsArrayFloat, bonesArrayInt, _positionsListFloat, _colorsListFloat,
					_bonesListInt);
				handle = cullInvisibleVoxelsJob.Schedule(positionsCount, handle);
				handle.Complete();
				UpdateVoxels();
			} else if (_cullingOptions == CullingOptions.InnerSides) {
				var cullInvisibleSidesJob = new CullInvisibleSidesJob(box, _sideIndex, positionsSlice, voxelBoxMasks,
					positionsArrayFloat, colorsArrayFloat, bonesArrayInt, _positionsListFloat, _colorsListFloat,
					_bonesListInt);
				handle = cullInvisibleSidesJob.Schedule(positionsCount, handle);
				handle.Complete();
				UpdateVoxels();
			} else if (_cullingOptions == CullingOptions.InnerSidesAndBackface || _cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate) {
				var bonesCount = visibilityBounds.Length / 6;
				var cullInvisibleSidesJob = new CullInvisibleSidesAndBackfaceJob(box, _sideIndex, positionsSlice, voxelBoxMasks,
					positionsArrayFloat, colorsArrayFloat, bonesArrayInt, _positionsListFloat.AsParallelWriter(), _colorsListFloat.AsParallelWriter(),
					_bonesListInt.AsParallelWriter(), new NativeSlice<VoxelsBounds>(visibilityBounds, bonesCount * _sideIndex, bonesCount));
				handle = cullInvisibleSidesJob.Schedule(positionsCount,
					/*positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, */handle);
				handle.Complete();
				UpdateVoxels();
			} else {
				handle.Complete();
				_positionsCount = positionsArrayFloat.Length;
				SetBufferData(VoxelPositionsBuffer, ref _positionBuffer, positionsArrayFloat, 12);
				SetBufferData(VoxelBonesBuffer, ref _bonesBuffer, bonesArrayInt, 4);
				SetBufferData(VoxelColorsBuffer, ref _colorsBuffer, colorsArrayFloat, 12);
			}
		}

		public void CullingUpdate(int positionsCount, VoxelsBox box, NativeSlice<byte3> positionsSlice,
			NativeArray<float3> positionsArrayFloat, NativeArray<float3> colorsArrayFloat,
			NativeArray<uint> bonesArrayInt, NativeArray<byte> voxelBoxMasks,
			NativeArray<VoxelsBounds> visibilityBounds, JobHandle handle) {
			
			_positionsListFloat.Clear();
			_bonesListInt.Clear();
			_colorsListFloat.Clear();
			
			var bonesCount = visibilityBounds.Length / 6;
			var cullInvisibleSidesJob = new CullInvisibleSidesAndBackfaceJob(box, _sideIndex, positionsSlice, voxelBoxMasks,
				positionsArrayFloat, colorsArrayFloat, bonesArrayInt, _positionsListFloat.AsParallelWriter(), _colorsListFloat.AsParallelWriter(),
				_bonesListInt.AsParallelWriter(), new NativeSlice<VoxelsBounds>(visibilityBounds, bonesCount * _sideIndex, bonesCount));
			_cullingHandle = cullInvisibleSidesJob.Schedule(positionsCount,
				/*positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, */handle);
		}
		
		public void UpdateVoxels() {
			_cullingHandle.Complete();
			_positionsCount = _positionsListFloat.Length;
			if (_positionsCount == 0) {
				_positionBuffer?.Dispose();
				_bonesBuffer?.Dispose();
				_colorsBuffer?.Dispose();
				_positionBuffer = null;
				_bonesBuffer = null;
				_colorsBuffer = null;
				return;
			}
			SetBufferData(VoxelPositionsBuffer, ref _positionBuffer, _positionsListFloat.AsArray(), 12);
			SetBufferData(VoxelBonesBuffer, ref _bonesBuffer, _bonesListInt.AsArray(), 4);
			SetBufferData(VoxelColorsBuffer, ref _colorsBuffer, _colorsListFloat.AsArray(), 12);
		}

		private void SetBufferData<T>(int nameId, ref ComputeBuffer buffer, NativeArray<T> data, int stride) where T : struct {
			buffer?.Dispose();
			buffer = new ComputeBuffer(data.Length, stride);
			buffer.SetData(data);
			_material.SetBuffer(nameId, buffer);
		}

		public void Dispose() {
			_cullingHandle.Complete();
			_positionBuffer?.Dispose();
			_bonesBuffer?.Dispose();
			_colorsBuffer?.Dispose();
			_bonePositionsBuffer?.Dispose();
			_bonePositionsAnimationBuffer?.Dispose();
			_boneRotationsAnimationBuffer?.Dispose();

			_positionsListFloat.Dispose();
			_colorsListFloat.Dispose();
			_bonesListInt.Dispose();
		}
	}
}