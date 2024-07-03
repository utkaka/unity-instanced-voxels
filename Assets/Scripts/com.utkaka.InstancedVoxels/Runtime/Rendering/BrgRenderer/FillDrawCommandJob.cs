using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer {
	[BurstCompile]
    public unsafe struct FillDrawCommandJob : IJob {
	    private readonly bool _castShadows;
	    private readonly BatchID _batchID;
	    private readonly BatchMaterialID _batchMaterialID;
	    private readonly BatchMeshID _batchMeshID;
	    [ReadOnly, DeallocateOnJobCompletion]
	    private NativeArray<int> _visibleSideVoxelsCount;
	    [ReadOnly, NativeDisableUnsafePtrRestriction]
	    private readonly int* _visibleSideVoxelsPointer;
	    [WriteOnly]
	    private NativeArray<BatchCullingOutputDrawCommands> _output;

	    public FillDrawCommandJob(NativeArray<BatchCullingOutputDrawCommands> cullingOutput, bool castShadows, BatchID batchID,
		    BatchMaterialID batchMaterialID, BatchMeshID batchMeshID, NativeArray<int> visibleSideVoxelsCount, int* visibleSideVoxelsPointer) {
		    _output = cullingOutput;
		    _castShadows = castShadows;
		    _batchID = batchID;
		    _batchMaterialID = batchMaterialID;
		    _batchMeshID = batchMeshID;
		    _visibleSideVoxelsCount = visibleSideVoxelsCount;
		    _visibleSideVoxelsPointer = visibleSideVoxelsPointer;
	    }

	    public void Execute() {
            var drawCommands = new BatchCullingOutputDrawCommands {
	            drawCommandCount = 1,
	            instanceSortingPositions = null,
	            instanceSortingPositionFloatCount = 0,
	            drawRangeCount = 1,
	            drawRanges = Malloc<BatchDrawRange>(1)
            };

            drawCommands.drawRanges[0] = new BatchDrawRange {
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

            drawCommands.visibleInstances = _visibleSideVoxelsPointer;
	                
            drawCommands.drawCommands = Malloc<BatchDrawCommand>(1);
            drawCommands.drawCommands[0] = new BatchDrawCommand {
	            visibleOffset = 0,
	            visibleCount = (uint)_visibleSideVoxelsCount[0],
	            batchID = _batchID,
	            materialID = _batchMaterialID,
	            meshID = _batchMeshID,
	            submeshIndex = 0,
	            splitVisibilityMask = 0xff,
	            flags = BatchDrawCommandFlags.None,
	            sortingPosition = 0
            };

            _output[0] = drawCommands;
	    }
	    
	    public static T* Malloc<T>(uint count) where T : unmanaged {
		    return (T*)UnsafeUtility.Malloc(
			    UnsafeUtility.SizeOf<T>() * count,
			    UnsafeUtility.AlignOf<T>(),
			    Allocator.TempJob);
	    }
    }
}