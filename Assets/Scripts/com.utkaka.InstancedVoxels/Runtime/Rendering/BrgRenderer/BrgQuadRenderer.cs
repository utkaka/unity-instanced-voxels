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
        
		private readonly int _sideIndex;
		private readonly Material _material;
		private readonly Mesh _mesh;

		private bool m_castShadows;
		private bool _isDirty;
		private int _voxelsCount;
		private NativeList<int> _sideVoxelsList;
		//private NativeList<int> _visibleIndices;
		
		private BatchRendererGroup _batchRendererGroup;
		
		private int m_instanceCount;
		private BatchID m_batchIDs;
		private BatchMeshID m_meshID;
		private BatchMaterialID m_materialID;
		private bool _refreshDrawCommands;
		//private int* _visibleInstances;


		public BrgQuadRenderer(int sideIndex, float voxelSize, CullingOptions cullingOptions, Material material) {
			m_castShadows = false;
			_refreshDrawCommands = true;
			_sideIndex = sideIndex;
			_mesh = VoxelMeshGenerator.GetSideMesh(sideIndex, voxelSize);
			_material = material;
		}
		
		public void InitVoxels(int positionsCount, VoxelsBox box, NativeList<int> outerIndices, NativeArray<ShaderVoxel> inputVoxels, NativeArray<byte> voxelBoxMasks, GraphicsBuffer graphicsBuffer) {
			_isDirty = true;
			_sideVoxelsList = new NativeList<int>(positionsCount, Allocator.Persistent);
			
			var cullInvisibleSidesJob = new CullInvisibleSidesIndicesJob(_sideIndex, box, outerIndices, inputVoxels, voxelBoxMasks, _sideVoxelsList);
			cullInvisibleSidesJob.Schedule(positionsCount, default).Complete();
			
			_batchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
			
	        m_instanceCount = _sideVoxelsList.Length;

	        // In our sample game we're dealing with 3 instanced properties: obj2world, world2obj and baseColor
	        var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

	        // Batch metadata buffer
	        var objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
	        var worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
	        var colorID = Shader.PropertyToID("_BaseColor");

	        // register one kind of batch per "window" in the large BRG raw buffer
	        batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0, true);       // matrices
	        batchMetadata[1] = CreateMetadataValue(worldToObjectID, positionsCount * 3 * 16, true); // inverse matrices
	        batchMetadata[2] = CreateMetadataValue(colorID, positionsCount * 3 * 2 * 16, true); // colors
	        m_batchIDs = _batchRendererGroup.AddBatch(batchMetadata, graphicsBuffer.bufferHandle,
		        0, 0);

	        // we don't need this metadata description array anymore
	        batchMetadata.Dispose();

	        // Setup very large bound to be sure BRG is never culled
	        var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
	        _batchRendererGroup.SetGlobalBounds(bounds);

	        // Register mesh and material
	        m_meshID = _batchRendererGroup.RegisterMesh(_mesh);
	        m_materialID = _batchRendererGroup.RegisterMaterial(_material);
		}
		
		
		
		private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
			BatchCullingOutput cullingOutput, IntPtr userContext) {
			
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
                    shadowCastingMode = m_castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = false,
                    staticShadowCaster = false,
                    allDepthSorted = false
                }
            };

            var jobHandle = new JobHandle();

            if (drawCommands.drawCommandCount > 0)
            {
                var visibleInstances = Malloc<int>((uint)m_instanceCount);
                
                /*if (!_refreshDrawCommands)
                {*/
	                UnsafeUtility.MemCpy(visibleInstances, _sideVoxelsList.GetUnsafePtr(),
		                (long)m_instanceCount * UnsafeUtility.SizeOf<int>());
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
	                visibleCount = (uint)m_instanceCount,
	                batchID = m_batchIDs,
	                materialID = m_materialID,
	                meshID = m_meshID,
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
		
		public void Dispose()
		{

			_sideVoxelsList.Dispose();
			_batchRendererGroup.RemoveBatch(m_batchIDs);

			_batchRendererGroup.UnregisterMaterial(m_materialID);
			_batchRendererGroup.UnregisterMesh(m_meshID);
			_batchRendererGroup.Dispose();
			//UnsafeUtility.Free(_visibleInstances, Allocator.Persistent);
		}
    }
}