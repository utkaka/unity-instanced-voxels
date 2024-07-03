using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
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
		private readonly VoxelsBox _box;
		private readonly NativeArray<ShaderVoxel> _voxels;
		private readonly NativeArray<byte> _voxelBoxMasks;
		
		private readonly bool _castShadows;
		
		private readonly BatchRendererGroup _batchRendererGroup;
		private readonly BatchMeshID _batchMeshID;
		private readonly BatchMaterialID _batchMaterialID;
		private BatchID _batchID;

		private JobHandle _updateOuterVoxelsHandle;
		
		private NativeList<int> _sideVoxelsList;
		//private NativeList<int> _visibleIndices;

		public BrgQuadRenderer(int sideIndex, float voxelSize, Material material, VoxelsBox box,
			NativeArray<ShaderVoxel> voxels, NativeArray<byte> voxelBoxMasks) {
			_castShadows = false;
			_sideIndex = sideIndex;
			_voxelSize = voxelSize;
			_box = box;
			_voxels = voxels;
			_voxelBoxMasks = voxelBoxMasks;

			_batchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
			//TODO: Maybe set more reasonable bounds?
			var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
			_batchRendererGroup.SetGlobalBounds(bounds);

			_batchMeshID = _batchRendererGroup.RegisterMesh(VoxelMeshGenerator.GetSideMesh(sideIndex, voxelSize));
			_batchMaterialID = _batchRendererGroup.RegisterMaterial(material);
		}

		public void UpdateOuterVoxels(int positionsCount, NativeArray<int> outerIndices, GraphicsBuffer graphicsBuffer) {
			_updateOuterVoxelsHandle.Complete();

			if (_sideVoxelsList.IsCreated) {
				_sideVoxelsList.Dispose();
			}
			
			_sideVoxelsList = new NativeList<int>(positionsCount, Allocator.Persistent);
			var cullInvisibleSidesJob = new CullInvisibleSidesIndicesJob(_sideIndex, _box, outerIndices, _voxels, _voxelBoxMasks, _sideVoxelsList);
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

			//if (!_sideVoxelsList.IsCreated) return default;
			
			/*var calculateVisibilityBoundsJob =
				new CalculateVisibilityBoundsJob(_voxelSize, _startPosition, VoxelMeshGenerator.GetSideNormal(_sideIndex), _box, cameraPosition, cameraForward,
					_animationLength, _animationCurrentFrame, _animationNextFrame, _animationLerpRatio,
					_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
					currentVisibilityBoundsSlice);
			var visibilityBoundsHandle = calculateVisibilityBoundsJob.Schedule(_bonesCount,
				_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount);
			
			var bonesCount = visibilityBounds.Length / 6;
			var cullBackfaceJob = new CullBackfaceJob(_visibleIndices.AsArray(), shaderVoxels,
				new NativeSlice<VoxelsBounds>(visibilityBounds, bonesCount * _sideIndex, bonesCount),
				_shaderVoxelsList);
			//_cullingHandle = handle;
			_cullingHandle = cullBackfaceJob.Schedule(_visibleIndices.Length,
				/*_visibleIndices.Length / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, #1#handle);*/
			
			_updateOuterVoxelsHandle.Complete();
			
			var drawCommands = new BatchCullingOutputDrawCommands();
            drawCommands.drawCommandCount = 1;

            // Allocate a single BatchDrawRange. ( all our draw commands will refer to this BatchDrawRange)
            drawCommands.drawRangeCount = 1;
            drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
            drawCommands.drawRanges[0] = new BatchDrawRange
            {
                drawCommandsBegin = 0,
                drawCommandsCount = 1,
                filterSettings = new BatchFilterSettings
                {
                    renderingLayerMask = 1,
                    layer = 0,
                    motionMode = MotionVectorGenerationMode.Camera,
                    shadowCastingMode = _castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = false,
                    staticShadowCaster = false,
                    allDepthSorted = false
                }
            };

            var jobHandle = new JobHandle();

            if (drawCommands.drawCommandCount > 0)
            {
                var visibleInstances = Malloc<int>((uint)_sideVoxelsList.Length);
                
                /*if (!_refreshDrawCommands)
                {*/
	                UnsafeUtility.MemCpy(visibleInstances, _sideVoxelsList.GetUnsafePtr(),
		                (long)_sideVoxelsList.Length * UnsafeUtility.SizeOf<int>());
	                drawCommands.visibleInstances = visibleInstances;
                /*} else {
	                _refreshDrawCommands = false;
	                _visibleInstances = Malloc<int>((uint)visibilityArraySize, Allocator.Persistent);
	                drawCommands.visibleInstances = visibleInstances;
			
	                var fillVisibleInstancesJob = new FillVisibleInstancesJob(_visibleInstances, visibleInstances, _sideVoxelsList);
	                jobHandle = fillVisibleInstancesJob.Schedule(visibilityArraySize,
		                visibilityArraySize / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, jobHandle);    
                }*/

                
                // Allocate the BatchDrawCommand array (drawCommandCount entries)
                // In SSBO mode, drawCommandCount will be just 1
                drawCommands.drawCommands = Malloc<BatchDrawCommand>(1);
                drawCommands.drawCommands[0] = new BatchDrawCommand {
	                visibleOffset = (uint)0,    // all draw command is using the same {0,1,2,3...} visibility int array
	                visibleCount = (uint)_sideVoxelsList.Length,
	                batchID = _batchID,
	                materialID = _batchMaterialID,
	                meshID = _batchMeshID,
	                submeshIndex = 0,
	                splitVisibilityMask = 0xff,
	                flags = BatchDrawCommandFlags.None,
	                sortingPosition = 0
                };
            }

            cullingOutput.drawCommands[0] = drawCommands;
            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;
            
            return jobHandle;
		}
		
		private static T* Malloc<T>(uint count, Allocator allocator = Allocator.TempJob) where T : unmanaged {
			return (T*)UnsafeUtility.Malloc(
				UnsafeUtility.SizeOf<T>() * count,
				UnsafeUtility.AlignOf<T>(),
				allocator);
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
			
			_sideVoxelsList.Dispose();
			
			_batchRendererGroup.RemoveBatch(_batchID);
			_batchRendererGroup.UnregisterMaterial(_batchMaterialID);
			_batchRendererGroup.UnregisterMesh(_batchMeshID);
			_batchRendererGroup.Dispose();
		}
    }
}