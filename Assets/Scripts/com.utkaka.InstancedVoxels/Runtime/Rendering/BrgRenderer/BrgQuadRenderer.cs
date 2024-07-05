using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public unsafe class BrgQuadRenderer {
		private readonly int _sideIndex;
		private readonly float _voxelSize;
		private readonly Vector3 _startPosition;
		private readonly int _bonesCount;
		private readonly int _animationLength;
		private readonly int _animationCurrentFrame;
		private readonly int _animationNextFrame;
		private float _animationLerpRatio;
		private readonly VoxelsBox _box;
		private readonly NativeArray<ShaderVoxel> _voxels;
		private readonly NativeArray<byte> _voxelBoxMasks;
		private NativeArray<VoxelsBounds> _visibilityBounds;
		
		private readonly NativeArray<float3> _bonePositionsArray;
		private readonly NativeArray<float3> _boneAnimationPositionsArray;
		private readonly NativeArray<float4> _boneAnimationRotationsArray;

		private JobHandle _updateOuterVoxelsHandle;
		private JobHandle _cullHandle;

		private NativeArray<int> _outerVoxelsIndices;
		private NativeList<int> _sideVoxelsIndices;
		
		public int SideVoxelsIndicesLength {
			get {
				_updateOuterVoxelsHandle.Complete();
				return _sideVoxelsIndices.Length;
			}
		}

		public BrgQuadRenderer(int sideIndex, float voxelSize, Vector3 startPosition, int bonesCount,
			int animationLength, Material material, VoxelsBox box,
			NativeArray<ShaderVoxel> voxels, NativeArray<byte> voxelBoxMasks, NativeArray<float3> bonePositionsArray,
			NativeArray<float3> boneAnimationPositionsArray, NativeArray<float4> boneAnimationRotationsArray) {
			_sideIndex = sideIndex;
			_voxelSize = voxelSize;
			_box = box;
			_voxels = voxels;
			_voxelBoxMasks = voxelBoxMasks;
			_bonePositionsArray = bonePositionsArray;
			_boneAnimationPositionsArray = boneAnimationPositionsArray;
			_boneAnimationRotationsArray = boneAnimationRotationsArray;
			_startPosition = startPosition;
			_bonesCount = bonesCount;
			_animationLength = animationLength;
			_animationCurrentFrame = 0;
			_animationNextFrame = 1;
			_animationLerpRatio = 0.0f;

			_visibilityBounds = new NativeArray<VoxelsBounds>(_bonesCount, Allocator.Persistent);
		}

		public void UpdateOuterVoxels(int positionsCount, NativeArray<int> outerIndices, GraphicsBuffer graphicsBuffer) {
			_updateOuterVoxelsHandle.Complete();

			_outerVoxelsIndices = outerIndices;

			if (_sideVoxelsIndices.IsCreated) {
				_sideVoxelsIndices.Dispose();
			}
			
			_sideVoxelsIndices = new NativeList<int>(positionsCount, Allocator.Persistent);
			var cullInvisibleSidesJob = new CullInvisibleSidesIndicesJob(_sideIndex, _box, outerIndices, _voxels, _voxelBoxMasks, _sideVoxelsIndices);
			_updateOuterVoxelsHandle = cullInvisibleSidesJob.Schedule(positionsCount, default);
		}
		
		public JobHandle OnPerformCulling(float3 cameraPosition, float3 cameraForward,
			int* visibleSideVoxelsArray, int offset, NativeArray<int> visibleSideVoxelsCount) {
			if (!_sideVoxelsIndices.IsCreated) return default;
			_updateOuterVoxelsHandle.Complete();
			
			var currentVisibilityBoundsSlice = new NativeSlice<VoxelsBounds>(_visibilityBounds, 0, _bonesCount);
			var calculateVisibilityBoundsJob =
				new CalculateVisibilityBoundsJob(_voxelSize, _startPosition, VoxelMeshGenerator.GetSideNormal(_sideIndex), _box, cameraPosition, cameraForward,
					_animationLength, _animationCurrentFrame, _animationNextFrame, _animationLerpRatio,
					_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
					currentVisibilityBoundsSlice);
			_cullHandle = calculateVisibilityBoundsJob.Schedule(_bonesCount,
				_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount);
			
			var cullBackfaceJob = new FillVisibleInstancesJob(_sideIndex, _sideVoxelsIndices.AsArray(), _outerVoxelsIndices, _voxels,  currentVisibilityBoundsSlice,
				visibleSideVoxelsArray, offset, visibleSideVoxelsCount);
			_cullHandle = cullBackfaceJob.Schedule(_sideVoxelsIndices.Length, _cullHandle);

            return _cullHandle;
		}
		
		public void Dispose() {
			_updateOuterVoxelsHandle.Complete();
			_cullHandle.Complete();
			
			_sideVoxelsIndices.Dispose();
			_visibilityBounds.Dispose();
		}
    }
}