using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public unsafe class BrgQuadRenderer {
        private static readonly int VoxelPositionsBuffer = Shader.PropertyToID("voxels_buffer");
        
        private const int InstanceSize = (3 + 3 + 1) * 16;
		
		private readonly int _sideIndex;
		private readonly Material _material;
		private readonly Mesh _mesh;
		private readonly CullingOptions _cullingOptions;

		private bool m_castShadows;
		private bool _isDirty;
		private int _voxelsCount;
		private JobHandle _cullingHandle;
		private ComputeBuffer _voxelsBuffer;
		private NativeList<ShaderVoxel> _shaderVoxelsList;
		private NativeList<int> _visibleIndices;
		
		private BatchRendererGroup _batchRendererGroup;
		
		
		private int m_instanceCount;
		private int m_maxInstances;
		private int m_alignedGPUWindowSize;
		private int m_maxInstancePerWindow;
		private int m_windowCount;
		private int m_totalGpuBufferSize;
		private GraphicsBuffer m_GPUPersistentInstanceData;
		private NativeArray<float4> m_sysmemBuffer;
		private BatchID[] m_batchIDs;
		private BatchMeshID m_meshID;
		private BatchMaterialID m_materialID;


		public BrgQuadRenderer(int sideIndex, float voxelSize, CullingOptions cullingOptions, Material material) {
			m_castShadows = false;
			_sideIndex = sideIndex;
			_cullingOptions = cullingOptions;
			_mesh = VoxelMeshGenerator.GetSideMesh(sideIndex, voxelSize);
			_material = material;
		}
		
		public void InitVoxels(int positionsCount, VoxelsBox box, NativeArray<ShaderVoxel> shaderVoxels, NativeArray<byte> voxelBoxMasks, NativeList<int> outerVoxels, JobHandle handle, float3 startPosition, float voxelSize) {
			_isDirty = true;
			_shaderVoxelsList = new NativeList<ShaderVoxel>(positionsCount, Allocator.Persistent);
			_visibleIndices = new NativeList<int>(positionsCount, Allocator.Persistent);
			
			var cullInvisibleSidesJob = new CullInvisibleSidesJob(box, _sideIndex, shaderVoxels, voxelBoxMasks, _shaderVoxelsList);
			handle = cullInvisibleSidesJob.Schedule(positionsCount, handle);
			handle.Complete();

			positionsCount = _shaderVoxelsList.Length;
			
			_batchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
			
	        m_instanceCount = positionsCount;
	        m_maxInstances = positionsCount;

	        // BRG uses a large GPU buffer. This is a RAW buffer on almost all platforms, and a constant buffer on GLES
	        // In case of constant buffer, we split it into several "windows" of BatchRendererGroup.GetConstantBufferMaxWindowSize() bytes each
	        if (BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer) {
	            m_alignedGPUWindowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();
	            m_maxInstancePerWindow = m_alignedGPUWindowSize / InstanceSize;
	            m_windowCount = (m_maxInstances + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
	            m_totalGpuBufferSize = m_windowCount * m_alignedGPUWindowSize;
	            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, m_totalGpuBufferSize / 16, 16);
	        }
	        else
	        {
	            m_alignedGPUWindowSize = m_maxInstances * InstanceSize;
	            m_maxInstancePerWindow = m_maxInstances;
	            m_windowCount = 1;
	            m_totalGpuBufferSize = m_windowCount * m_alignedGPUWindowSize;
	            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_totalGpuBufferSize / 4, 4);
	        }

	        // In our sample game we're dealing with 3 instanced properties: obj2world, world2obj and baseColor
	        var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

	        // Batch metadata buffer
	        var objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
	        var worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
	        var colorID = Shader.PropertyToID("_BaseColor");

	        // Create system memory copy of big GPU raw buffer
	        m_sysmemBuffer = new NativeArray<float4>(m_totalGpuBufferSize / 16, Allocator.Persistent, NativeArrayOptions.ClearMemory);

	        // register one kind of batch per "window" in the large BRG raw buffer
	        m_batchIDs = new BatchID[m_windowCount];
	        for (var b = 0; b < m_windowCount; b++)
	        {
	            batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0, true);       // matrices
	            batchMetadata[1] = CreateMetadataValue(worldToObjectID, m_maxInstancePerWindow * 3 * 16, true); // inverse matrices
	            batchMetadata[2] = CreateMetadataValue(colorID, m_maxInstancePerWindow * 3 * 2 * 16, true); // colors
	            var offset = b * m_alignedGPUWindowSize;
	            m_batchIDs[b] = _batchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle,
		            (uint)offset,
		            BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer
			            ? (uint)m_alignedGPUWindowSize
			            : 0);
	        }

	        // we don't need this metadata description array anymore
	        batchMetadata.Dispose();

	        // Setup very large bound to be sure BRG is never culled
	        var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
	        _batchRendererGroup.SetGlobalBounds(bounds);

	        // Register mesh and material
	        m_meshID = _batchRendererGroup.RegisterMesh(_mesh);
	        m_materialID = _batchRendererGroup.RegisterMaterial(_material);
			
	        var updatePositionsJob = new UpdatePositionsJob(startPosition, voxelSize, positionsCount, _shaderVoxelsList, m_sysmemBuffer);
	        updatePositionsJob.Schedule(positionsCount,
		        positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle).Complete();
	        UploadGpuData(positionsCount);
		}
		
		[BurstCompile]
		public bool UploadGpuData(int instanceCount) {
			if ((uint)instanceCount > (uint)m_maxInstances)
				return false;

			m_instanceCount = instanceCount;
			var completeWindows = m_instanceCount / m_maxInstancePerWindow;

			// update all complete windows in one go
			if (completeWindows > 0)
			{
				var sizeInFloat4 = (completeWindows * m_alignedGPUWindowSize) / 16;
				m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, 0, 0, sizeInFloat4);
			}

			// then upload data for the last (incomplete) window
			var lastBatchId = completeWindows;
			var itemInLastBatch = m_instanceCount - m_maxInstancePerWindow * completeWindows;

			if (itemInLastBatch > 0)
			{
				var windowOffsetInFloat4 = (lastBatchId * m_alignedGPUWindowSize) / 16;
				var offsetMat1 = windowOffsetInFloat4 + m_maxInstancePerWindow * 0;
				var offsetMat2 = windowOffsetInFloat4 + m_maxInstancePerWindow * 3;
				var offsetColor = windowOffsetInFloat4 + m_maxInstancePerWindow * 3 * 2;
				m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat1, offsetMat1, itemInLastBatch * 3);     // 3 float4 for obj2world
				m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat2, offsetMat2, itemInLastBatch * 3);    // 3 float4 for world2obj
				m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetColor, offsetColor, itemInLastBatch * 1);     // 1 float4 for color
			}

			return true;
		}
		
		private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
			BatchCullingOutput cullingOutput, IntPtr userContext) {
			var drawCommands = new BatchCullingOutputDrawCommands();

            // calculate the amount of draw commands we need in case of UBO mode (one draw command per window)
            var drawCommandCount = (m_instanceCount + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
            var maxInstancePerDrawCommand = m_maxInstancePerWindow;
            drawCommands.drawCommandCount = drawCommandCount;

            // Allocate a single BatchDrawRange. ( all our draw commands will refer to this BatchDrawRange)
            drawCommands.drawRangeCount = 1;
            drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
            drawCommands.drawRanges[0] = new BatchDrawRange
            {
                drawCommandsBegin = 0,
                drawCommandsCount = (uint)drawCommandCount,
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

            if (drawCommands.drawCommandCount > 0)
            {
                // as we don't need culling, the visibility int array buffer will always be {0,1,2,3,...} for each draw command
                // so we just allocate maxInstancePerDrawCommand and fill it
                var visibilityArraySize = maxInstancePerDrawCommand;
                if (m_instanceCount < visibilityArraySize)
                    visibilityArraySize = m_instanceCount;

                drawCommands.visibleInstances = Malloc<int>((uint)visibilityArraySize);

                // As we don't need any frustum culling in our context, we fill the visibility array with {0,1,2,3,...}
                for (var i = 0; i < visibilityArraySize; i++)
                    drawCommands.visibleInstances[i] = i;

                // Allocate the BatchDrawCommand array (drawCommandCount entries)
                // In SSBO mode, drawCommandCount will be just 1
                drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
                var left = m_instanceCount;
                for (var b = 0; b < drawCommandCount; b++)
                {
                    var inBatchCount = left > maxInstancePerDrawCommand ? maxInstancePerDrawCommand : left;
                    drawCommands.drawCommands[b] = new BatchDrawCommand
                    {
                        visibleOffset = (uint)0,    // all draw command is using the same {0,1,2,3...} visibility int array
                        visibleCount = (uint)inBatchCount,
                        batchID = m_batchIDs[b],
                        materialID = m_materialID,
                        meshID = m_meshID,
                        submeshIndex = 0,
                        splitVisibilityMask = 0xff,
                        flags = BatchDrawCommandFlags.None,
                        sortingPosition = 0
                    };
                    left -= inBatchCount;
                }
            }

            cullingOutput.drawCommands[0] = drawCommands;
            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;
            
            return new JobHandle();
		}
		
		private static T* Malloc<T>(uint count) where T : unmanaged {
			return (T*)UnsafeUtility.Malloc(
				UnsafeUtility.SizeOf<T>() * count,
				UnsafeUtility.AlignOf<T>(),
				Allocator.TempJob);
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
			_cullingHandle.Complete();
			_voxelsBuffer?.Dispose();
			_shaderVoxelsList.Dispose();
			_visibleIndices.Dispose();
			
			for (uint b = 0; b < m_windowCount; b++)
				_batchRendererGroup.RemoveBatch(m_batchIDs[b]);

			_batchRendererGroup.UnregisterMaterial(m_materialID);
			_batchRendererGroup.UnregisterMesh(m_meshID);
			_batchRendererGroup.Dispose();
			m_GPUPersistentInstanceData.Dispose();
			m_sysmemBuffer.Dispose();
		}
    }
}