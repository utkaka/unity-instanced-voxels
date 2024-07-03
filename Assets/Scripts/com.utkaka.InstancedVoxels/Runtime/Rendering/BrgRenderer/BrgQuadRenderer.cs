using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public unsafe class BrgQuadRenderer {
	    private static readonly int ShaderObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
	    private static readonly int  ShaderWorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
	    private static readonly int  ShaderColorID = Shader.PropertyToID("_BaseColor");
        
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
		
		private readonly bool _castShadows;
		
		private readonly BatchRendererGroup _batchRendererGroup;
		private readonly BatchMeshID _batchMeshID;
		private readonly BatchMaterialID _batchMaterialID;
		private BatchID _batchID;

		private JobHandle _updateOuterVoxelsHandle;

		private NativeArray<int> _outerVoxelsIndices;
		private NativeList<int> _sideVoxelsIndices;

		public BrgQuadRenderer(int sideIndex, float voxelSize, Vector3 startPosition, int bonesCount,
			int animationLength, Material material, VoxelsBox box,
			NativeArray<ShaderVoxel> voxels, NativeArray<byte> voxelBoxMasks, NativeArray<float3> bonePositionsArray,
			NativeArray<float3> boneAnimationPositionsArray, NativeArray<float4> boneAnimationRotationsArray) {
			_castShadows = false;
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

			_batchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
			//TODO: Maybe set more reasonable bounds?
			var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
			_batchRendererGroup.SetGlobalBounds(bounds);

			_batchMeshID = _batchRendererGroup.RegisterMesh(VoxelMeshGenerator.GetSideMesh(sideIndex, voxelSize));
			_batchMaterialID = _batchRendererGroup.RegisterMaterial(material);
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
			
			_batchRendererGroup.RemoveBatch(_batchID);
			var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			batchMetadata[0] = CreateMetadataValue(ShaderObjectToWorldID, 0, true);       // matrices
			batchMetadata[1] = CreateMetadataValue(ShaderWorldToObjectID, positionsCount * 3 * 16, true); // inverse matrices
			batchMetadata[2] = CreateMetadataValue(ShaderColorID, positionsCount * 3 * 2 * 16, true); // colors
			_batchID = _batchRendererGroup.AddBatch(batchMetadata, graphicsBuffer.bufferHandle,
				0, 0);
			batchMetadata.Dispose();
		}
		
		private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
			BatchCullingOutput cullingOutput, IntPtr userContext) {

			_updateOuterVoxelsHandle.Complete();

			var visibleSideVoxelsArray = FillDrawCommandJob.Malloc<int>((uint)_sideVoxelsIndices.Length);
			var visibleSideVoxelsCount = new NativeArray<int>(1, Allocator.TempJob);

			var camera = Camera.main;
			var cameraPosition = camera.transform.position;
			var cameraForward = camera.transform.forward;
			
			var currentVisibilityBoundsSlice = new NativeSlice<VoxelsBounds>(_visibilityBounds, 0, _bonesCount);
			
			var calculateVisibilityBoundsJob =
				new CalculateVisibilityBoundsJob(_voxelSize, _startPosition, VoxelMeshGenerator.GetSideNormal(_sideIndex), _box, cameraPosition, cameraForward,
					_animationLength, _animationCurrentFrame, _animationNextFrame, _animationLerpRatio,
					_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
					currentVisibilityBoundsSlice);
			var handle = calculateVisibilityBoundsJob.Schedule(_bonesCount,
				_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount);
			
			var cullBackfaceJob = new FillVisibleInstancesJob(_sideVoxelsIndices.AsArray(), _outerVoxelsIndices, _voxels,  currentVisibilityBoundsSlice,
				visibleSideVoxelsArray, visibleSideVoxelsCount);
			handle = cullBackfaceJob.Schedule(_sideVoxelsIndices.Length, handle);

			var fillDrawCommandJob = new FillDrawCommandJob(cullingOutput.drawCommands, _castShadows, _batchID, _batchMaterialID,
				_batchMeshID, visibleSideVoxelsCount, visibleSideVoxelsArray);

			handle = fillDrawCommandJob.Schedule(handle);
            
            return handle;
		}
		
		private static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance) {
			const uint kIsPerInstanceBit = 0x80000000;
			return new MetadataValue
			{
				NameID = nameID,
				Value = (uint)gpuOffset | (isPerInstance ? (kIsPerInstanceBit) : 0),
			};
		}
		
		public void Dispose() {
			_updateOuterVoxelsHandle.Complete();
			
			_sideVoxelsIndices.Dispose();
			_visibilityBounds.Dispose();
			
			_batchRendererGroup.RemoveBatch(_batchID);
			_batchRendererGroup.UnregisterMaterial(_batchMaterialID);
			_batchRendererGroup.UnregisterMesh(_batchMeshID);
			_batchRendererGroup.Dispose();
		}
    }
}