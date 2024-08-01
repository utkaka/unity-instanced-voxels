using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
		private NativeArray<VoxelsBounds> _previousVisibilityBounds;
		private NativeArray<int> _previousVisibileIndices;
		private NativeArray<VoxelsBounds> _visibilityBounds;
		
		private readonly NativeArray<float3> _bonePositionsArray;
		private readonly NativeArray<float3> _boneAnimationPositionsArray;
		private readonly NativeArray<float4> _boneAnimationRotationsArray;

		private JobHandle _updateOuterVoxelsHandle;

		private NativeArray<int> _outerVoxelsIndices;
		private NativeList<int> _sideVoxelsIndices;
		
		public int SideVoxelsIndicesLength {
			get {
				_updateOuterVoxelsHandle.Complete();
				return _sideVoxelsIndices.Length;
			}
		}

		public BrgQuadRenderer(int sideIndex, float voxelSize, Vector3 startPosition, int bonesCount,
			int animationLength, VoxelsBox box,
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
			_previousVisibilityBounds = new NativeArray<VoxelsBounds>(_bonesCount, Allocator.Persistent);
		}

		public void UpdateOuterVoxels(int positionsCount, NativeArray<int> outerIndices) {
			_updateOuterVoxelsHandle.Complete();

			_outerVoxelsIndices = outerIndices;

			if (_sideVoxelsIndices.IsCreated) {
				_sideVoxelsIndices.Dispose();
			}

			if (_previousVisibileIndices.IsCreated) {
				_previousVisibileIndices.Dispose();
			}
			
			_sideVoxelsIndices = new NativeList<int>(positionsCount, Allocator.Persistent);
			var cullInvisibleSidesJob = new CullInvisibleSidesIndicesJob(_sideIndex, _box, outerIndices, _voxels, _voxelBoxMasks, _sideVoxelsIndices);
			_updateOuterVoxelsHandle = cullInvisibleSidesJob.Schedule(positionsCount, default);
		}
		
		public JobHandle OnPerformCulling(float3 cameraPosition, float3 cameraForward,
			int* visibleSideVoxelsArray, NativeArray<int> offsets, NativeArray<int> visibleSideVoxelsCount,
			int batchCount, int itemsPerWindow) {
			if (!_sideVoxelsIndices.IsCreated) return default;
			_updateOuterVoxelsHandle.Complete();
			NativeArray<VoxelsBounds>.Copy(_visibilityBounds, _previousVisibilityBounds);
			var previousVisibilityBoundsSlice = new NativeSlice<VoxelsBounds>(_previousVisibilityBounds, 0, _bonesCount);
			var currentVisibilityBoundsSlice = new NativeSlice<VoxelsBounds>(_visibilityBounds, 0, _bonesCount);
			var calculateVisibilityBoundsJob =
				new CalculateVisibilityBoundsJob(_voxelSize, _startPosition, VoxelMeshGenerator.GetSideNormal(_sideIndex), _box, cameraPosition, cameraForward,
					_animationLength, _animationCurrentFrame, _animationNextFrame, _animationLerpRatio,
					_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
					currentVisibilityBoundsSlice);
			
			var visibilityBoundsHandle = calculateVisibilityBoundsJob.Schedule(_bonesCount,
				_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount);
			//TODO: if there is no _previousVisibileIndices we can skip CheckVisibilityBoundsChangedJob
			var boundsChanged = new NativeArray<bool>(1, Allocator.TempJob);
			var checkVisibilityBoundsChangedJob = new CheckVisibilityBoundsChangedJob(previousVisibilityBoundsSlice,
				currentVisibilityBoundsSlice, boundsChanged);
			visibilityBoundsHandle = checkVisibilityBoundsChangedJob.Schedule(_bonesCount, visibilityBoundsHandle);
			visibilityBoundsHandle.Complete();

			var handle = default(JobHandle);
			if (boundsChanged[0] || !_previousVisibileIndices.IsCreated) {
				for (var i = 0; i < batchCount; i++) {
					visibleSideVoxelsCount[_sideIndex * batchCount + i] = 0;	
				}
				if (!_previousVisibileIndices.IsCreated) {
					_previousVisibileIndices = new NativeArray<int>(SideVoxelsIndicesLength, Allocator.Persistent);	
				}
				var cullBackfaceJob = new FillVisibleInstancesJob(_sideIndex, itemsPerWindow, batchCount, _sideVoxelsIndices.AsArray(), _outerVoxelsIndices, _voxels,  currentVisibilityBoundsSlice,
					visibleSideVoxelsArray + offsets[batchCount * _sideIndex], visibleSideVoxelsCount, _previousVisibileIndices, offsets);
				handle = cullBackfaceJob.Schedule(_sideVoxelsIndices.Length, handle);
			} else {
				var pointerWithOffset = visibleSideVoxelsArray + offsets[batchCount * _sideIndex];
				UnsafeUtility.MemCpy(pointerWithOffset, _previousVisibileIndices.GetUnsafePtr(),
					(long)SideVoxelsIndicesLength * UnsafeUtility.SizeOf<int>());
			}
			boundsChanged.Dispose();
            return handle;
		}
		
		public void Dispose() {
			_updateOuterVoxelsHandle.Complete();
			_previousVisibilityBounds.Dispose();
			_previousVisibileIndices.Dispose();
			_sideVoxelsIndices.Dispose();
			_visibilityBounds.Dispose();
		}
    }
}