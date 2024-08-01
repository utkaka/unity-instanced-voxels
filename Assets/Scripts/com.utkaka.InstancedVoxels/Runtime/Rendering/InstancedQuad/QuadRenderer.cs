using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad {
	public class QuadRenderer : IDisposable {
		private static readonly int VoxelPositionsBuffer = Shader.PropertyToID("voxels_buffer");
		
		private readonly int _sideIndex;
		private readonly Material _material;
		private readonly Mesh _mesh;
		private readonly CullingOptions _cullingOptions;

		private bool _isDirty;
		private int _voxelsCount;
		private JobHandle _cullingHandle;
		private ComputeBuffer _voxelsBuffer;
		private NativeList<ShaderVoxel> _shaderVoxelsList;
		private NativeList<int> _visibleIndices;


		public QuadRenderer(int sideIndex, float voxelSize, CullingOptions cullingOptions) {
			_sideIndex = sideIndex;
			_cullingOptions = cullingOptions;
			_mesh = VoxelMeshGenerator.GetSideMesh(sideIndex, voxelSize);
			_material = new Material(Shader.Find("Shader Graphs/InstancedVoxelShader"));
		}

		public void Render(Bounds bounds) {
			if (_voxelsCount == 0) return;
			Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material,
				bounds, _voxelsCount, null, ShadowCastingMode.Off, false);
		}

		public void InitVoxels(int positionsCount, VoxelsBox box, NativeArray<ShaderVoxel> shaderVoxels, NativeArray<byte> voxelBoxMasks,
			NativeArray<VoxelsBounds> visibilityBounds, NativeList<int> outerVoxels, JobHandle handle) {
			_isDirty = true;
			_shaderVoxelsList = new NativeList<ShaderVoxel>(positionsCount, Allocator.Persistent);
			_visibleIndices = new NativeList<int>(positionsCount, Allocator.Persistent);
			
			if (_cullingOptions == CullingOptions.InnerSides) {
				var cullInvisibleSidesJob = new CullInvisibleSidesJob(box, _sideIndex, shaderVoxels, voxelBoxMasks, _shaderVoxelsList);
				handle = cullInvisibleSidesJob.Schedule(positionsCount, handle);
				handle.Complete();
				UpdateVoxels();
			} else {
				var bonesCount = visibilityBounds.Length / 6;
				handle.Complete();
				
				var cullInvisibleSidesJob = new CullInvisibleSidesIndicesJob(_sideIndex, box, outerVoxels.AsArray(), shaderVoxels,
					voxelBoxMasks, _visibleIndices);
				cullInvisibleSidesJob.Schedule(outerVoxels.Length, default).Complete();

				var cullBackfaceJob = new CullBackfaceJob(_visibleIndices.AsArray(), shaderVoxels,
					new NativeSlice<VoxelsBounds>(visibilityBounds, bonesCount * _sideIndex, bonesCount),
					_shaderVoxelsList);
				handle = cullBackfaceJob.Schedule(_visibleIndices.Length, default);
					/*positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, );*/
				handle.Complete();
				UpdateVoxels();
			}
		}

		public void CullingUpdate(NativeArray<ShaderVoxel> shaderVoxels, NativeArray<VoxelsBounds> visibilityBounds, JobHandle handle) {
			_isDirty = true;
			//if (!_cullingHandle.IsCompleted) return;
			
			_shaderVoxelsList.Clear();

			var bonesCount = visibilityBounds.Length / 6;
			var cullBackfaceJob = new CullBackfaceJob(_visibleIndices.AsArray(), shaderVoxels,
				new NativeSlice<VoxelsBounds>(visibilityBounds, bonesCount * _sideIndex, bonesCount),
				_shaderVoxelsList);
			//_cullingHandle = handle;
			_cullingHandle = cullBackfaceJob.Schedule(_visibleIndices.Length,
				/*_visibleIndices.Length / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, */handle);
		}
		
		public void UpdateVoxels() {
			if (!_isDirty) return;
			_isDirty = false;
			//if (!_cullingHandle.IsCompleted) return;
			_cullingHandle.Complete();
			_voxelsCount = _shaderVoxelsList.Length;
			if (_voxelsCount == 0) {
				_voxelsBuffer?.Dispose();
				_voxelsBuffer = null;
				return;
			}
			SetBufferData(VoxelPositionsBuffer, ref _voxelsBuffer, _shaderVoxelsList.AsArray(), 8);
		}

		private void SetBufferData<T>(int nameId, ref ComputeBuffer buffer, NativeArray<T> data, int stride) where T : struct {
			buffer?.Dispose();
			buffer = new ComputeBuffer(data.Length, stride);
			buffer.SetData(data);
			_material.SetBuffer(nameId, buffer);
		}

		public void Dispose() {
			_cullingHandle.Complete();
			_voxelsBuffer?.Dispose();
			_shaderVoxelsList.Dispose();
			_visibleIndices.Dispose();
		}
	}
}