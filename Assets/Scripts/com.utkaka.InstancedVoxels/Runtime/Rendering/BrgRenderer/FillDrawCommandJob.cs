using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer {
	[BurstCompile]
    public unsafe struct FillDrawCommandJob : IJob {
	    private readonly bool _castShadows;
	    private readonly BatchMaterialID _batchMaterialID;
	    private readonly int _instancesPerBatch;
	    private readonly int _batchesCount;
	    private readonly NativeArray<BatchID> _batchesIDs;
	    private readonly NativeArray<BatchMeshID> _batchMeshIDs;
	    [ReadOnly, NativeDisableUnsafePtrRestriction]
	    private readonly int* _visibleSideVoxelsPointer;
	    [ReadOnly]
	    private NativeArray<int> _visibleSideVoxelsCount;
	    [ReadOnly]
	    private readonly NativeArray<int> _visibleSideVoxelsOffset;
	    [WriteOnly]
	    private NativeArray<BatchCullingOutputDrawCommands> _output;

	    private readonly int _positionsCount;

	    public FillDrawCommandJob(NativeArray<BatchCullingOutputDrawCommands> cullingOutput, bool castShadows, NativeArray<BatchID> batchesIDs,
		    BatchMaterialID batchMaterialID, NativeArray<BatchMeshID> batchMeshIDs,
		    int* visibleSideVoxelsPointer, NativeArray<int> visibleSideVoxelsOffset,
		    NativeArray<int> visibleSideVoxelsCount, int instancesPerBatch, int batchesCount, int positionsCount) {
		    _output = cullingOutput;
		    _castShadows = castShadows;
		    _batchesIDs = batchesIDs;
		    _batchMaterialID = batchMaterialID;
		    _batchMeshIDs = batchMeshIDs;
		    _visibleSideVoxelsPointer = visibleSideVoxelsPointer;
		    _visibleSideVoxelsCount = visibleSideVoxelsCount;
		    _instancesPerBatch = instancesPerBatch;
		    _batchesCount = batchesCount;
		    _visibleSideVoxelsOffset = visibleSideVoxelsOffset;
		    _positionsCount = positionsCount;
	    }

	    public void Execute() {
		    var drawCommandsCount = 6 * (_batchesCount);
            var drawCommands = new BatchCullingOutputDrawCommands {
	            drawCommandCount = drawCommandsCount,
	            instanceSortingPositions = null,
	            instanceSortingPositionFloatCount = 0,
	            drawRangeCount = 1,
	            drawRanges = Malloc<BatchDrawRange>(1)
            };

            drawCommands.drawRanges[0] = new BatchDrawRange {
                drawCommandsBegin = 0,
                drawCommandsCount = (uint)drawCommandsCount,
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
	                
            drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandsCount);
            for (var i = 0; i < 6; i++) {
	            for (var j = 0; j < _batchesCount; j++) {
		            drawCommands.drawCommands[i * _batchesCount + j] = new BatchDrawCommand {
			            visibleOffset = (uint)_visibleSideVoxelsOffset[i * _batchesCount + j],
			            visibleCount = (uint)_visibleSideVoxelsCount[i * _batchesCount + j],
			            batchID = _batchesIDs[j],
			            materialID = _batchMaterialID,
			            meshID = _batchMeshIDs[i],
			            submeshIndex = 0,
			            splitVisibilityMask = 0xff,
			            flags = BatchDrawCommandFlags.None,
			            sortingPosition = 0
		            };
	            }
            }

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