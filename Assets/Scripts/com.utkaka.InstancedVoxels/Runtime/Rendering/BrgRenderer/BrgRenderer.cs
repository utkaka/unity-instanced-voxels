using System;
using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer {
    public abstract unsafe class BrgRenderer : MonoBehaviour, IVoxelRenderer {
		[SerializeField]
		protected Voxels _voxels;
		[SerializeField]
		protected Material _material;
		private readonly bool _castShadows;
		
		protected Bounds _bounds;

		protected float _voxelSize;
		protected Vector3 _startPosition;
		private VoxelsBox _box;
		private int _positionsCount;
		private int _bonesCount;
		
		protected float _animationFrameRate;
		protected float _animationTime;
		protected int _animationLength;
		
		protected int _animationCurrentFrame;
		protected int _animationNextFrame;
		protected float _animationLerpRatio;
		
		protected BrgQuadRenderer[] _quadRenderers;
		
		protected NativeList<ShaderVoxel> _shaderVoxelsArray;
		protected NativeArray<byte> _voxelBoxMasks;
		protected NativeList<int> _outerVoxels;
		
		protected NativeArray<float3> _bonePositionsArray;
		protected NativeArray<float3> _boneAnimationPositionsArray;
		protected NativeArray<float4> _boneAnimationRotationsArray;

		protected BatchMetadata BatchMetadata;

		private BatchRendererGroup _batchRendererGroup;
		protected GraphicsBuffer _graphicsBuffer;
		private NativeArray<BatchID> _batchIDs;
		private BatchMaterialID _batchMaterialID;
		private NativeArray<BatchMeshID> _batchMeshIDs;
		private NativeArray<int> _visibleSideVoxelsCount;
		private NativeArray<int> _visibleVoxelsOffsets;

		public void Init(Voxels voxels, CullingOptions cullingOptions) {
			_voxels = voxels;
			if (_material == null) {
				_material = GetDefaultMaterial();
			}
		}
		
		private void Start() {
			InitVoxels();
		}

		protected abstract Material GetDefaultMaterial();

		protected virtual void InitVoxels() {
			_voxelSize = _voxels.VoxelSize;
			_startPosition = _voxels.StartPosition;
			_bounds = new Bounds(Vector3.zero, 
				new Vector3(_voxels.Box.Size.x, _voxels.Box.Size.y, _voxels.Box.Size.z) * _voxels.VoxelSize);
			
			_box = new VoxelsBox(_voxels.Box.Size + new int3(2, 2, 2));
			
			_animationFrameRate = _voxels.Animation.FrameRate;
			_animationLength = _voxels.Animation.FramesCount;
			
			var plainVoxelsArray = new NativeArray<byte>(_voxels.PlainVoxels, Allocator.TempJob);
			var plainVoxelsSlice = new NativeSlice<byte>(plainVoxelsArray).SliceConvert<VoxelPlain>();
			var compressedVoxelsArray = new NativeArray<byte>(_voxels.CompressedVoxels, Allocator.TempJob);
			var compressedVoxelsSlice = new NativeSlice<byte>(compressedVoxelsArray).SliceConvert<VoxelCompressed>();

			_bonePositionsArray = _voxels.Animation.BonesPositions.Length == 0
				? new NativeArray<float3>(2, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.BonesPositions, Allocator.Persistent);
			_boneAnimationPositionsArray = _voxels.Animation.AnimationBonesPositions.Length == 0
				? new NativeArray<float3>(2, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.AnimationBonesPositions, Allocator.Persistent);
			_boneAnimationRotationsArray = _voxels.Animation.AnimationBonesRotations.Length == 0
				? new NativeArray<float4>(2, Allocator.Persistent)
				: new NativeArray<float4>(_voxels.Animation.AnimationBonesRotations, Allocator.Persistent);
			
			_shaderVoxelsArray = new NativeList<ShaderVoxel>(_box.Count, Allocator.Persistent);
			_voxelBoxMasks = new NativeArray<byte>(_box.Count, Allocator.Persistent);
			var voxelBoxBones = new NativeArray<byte>(_box.Count, Allocator.TempJob);

			var setupPlainVoxelsJob = new SetupRuntimePlainVoxelsJob(_box, plainVoxelsSlice,
				_shaderVoxelsArray, _voxelBoxMasks, voxelBoxBones);
			var handle = setupPlainVoxelsJob.Schedule(plainVoxelsSlice.Length, default);
			var setupCompressedVoxelsJob = new SetupRuntimeCompressedVoxelsJob(_box, compressedVoxelsSlice,
				_shaderVoxelsArray, _voxelBoxMasks, voxelBoxBones);
			handle = setupCompressedVoxelsJob.Schedule(compressedVoxelsSlice.Length, handle);
			handle.Complete();
			
			_positionsCount = _shaderVoxelsArray.Length;
			var boneMasks = new NativeArray<byte>(_positionsCount, Allocator.TempJob);
			
			var maskSameBoneJob = new MaskSameBoneJob(_box, _shaderVoxelsArray, voxelBoxBones, boneMasks);
			var maskVoxelSidesJob = new MaskVoxelSidesJob(_box, _shaderVoxelsArray, boneMasks, _voxelBoxMasks);

			var sliceSize = _positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			
			handle = maskSameBoneJob.Schedule(_positionsCount, sliceSize, handle);
			handle = maskVoxelSidesJob.Schedule(_positionsCount, sliceSize, handle);

			_bonesCount = _bonePositionsArray.Length;
			
			_quadRenderers = new BrgQuadRenderer[6];
			
			_batchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
			//TODO: Maybe set more reasonable bounds?
			var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
			_batchRendererGroup.SetGlobalBounds(bounds);
			_batchRendererGroup.SetEnabledViewTypes(new []{ BatchCullingViewType.Camera});
			_batchMaterialID = _batchRendererGroup.RegisterMaterial(_material);

			_batchMeshIDs = new NativeArray<BatchMeshID>(6, Allocator.Persistent);
			for (var i = 0; i < 6; i++) {
				_batchMeshIDs[i] = _batchRendererGroup.RegisterMesh(VoxelMeshGenerator.GetSideMesh(i, _voxelSize));
			}
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i] = new BrgQuadRenderer(i, _voxelSize, _startPosition, _bonesCount, _animationLength, _box, _shaderVoxelsArray, _voxelBoxMasks, _bonePositionsArray,
					_boneAnimationPositionsArray, _boneAnimationRotationsArray);
			}
			
			UpdateOuterVoxels(handle);
			
			plainVoxelsArray.Dispose();
			compressedVoxelsArray.Dispose();

			voxelBoxBones.Dispose();
			boneMasks.Dispose();
			
			_voxels = null;
		}

		private void UpdateOuterVoxels(JobHandle handle) {
			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_outerVoxels = new NativeList<int>(_positionsCount, Allocator.Persistent);
			var cullInnerVoxelsJob = new CullInnerVoxelsJob(_box, _shaderVoxelsArray, _voxelBoxMasks, _outerVoxels);
			handle = cullInnerVoxelsJob.Schedule(_positionsCount, handle);
			handle.Complete();
			
			var outerVoxelsCount = _outerVoxels.Length;

			CreateBuffer(outerVoxelsCount, handle);
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].UpdateOuterVoxels(outerVoxelsCount, _outerVoxels);
			}

			CreateBatches(outerVoxelsCount);
		}

		private void CreateBuffer(int positionsCount, JobHandle handle) {
			_graphicsBuffer?.Dispose();
			var bufferSize = BatchMetadata.GetBufferSize(positionsCount);
			var cpuGraphicsBuffer = new NativeArray<byte>(bufferSize, Allocator.Temp);
			var cpuGraphicsBufferPointer = (byte*)cpuGraphicsBuffer.GetUnsafePtr();

			var fillBufferHandles = default(JobHandle);
			var indexOffset = 0;
			
			while (positionsCount > 0) {
				var batchPositionsCount = BatchMetadata.ItemsPerWindow > 0
					? Mathf.Min(BatchMetadata.ItemsPerWindow, positionsCount)
					: positionsCount;
				
				var offset = 0;
				for (var i = 0; i < BatchMetadata.Length; i++) {
					var metadataValue = BatchMetadata.GetValue(i);
					metadataValue.SetValueToBuffer(cpuGraphicsBufferPointer, offset);
					offset += metadataValue.GetBufferSize(batchPositionsCount);
				}

				fillBufferHandles = JobHandle.CombineDependencies(fillBufferHandles,
					FillBuffer(batchPositionsCount, indexOffset, cpuGraphicsBufferPointer, handle));
				
				
				cpuGraphicsBufferPointer += BatchRendererGroup.GetConstantBufferMaxWindowSize();
				positionsCount -= batchPositionsCount;
				indexOffset += batchPositionsCount;
			}
			
			fillBufferHandles.Complete();

			_graphicsBuffer = BatchMetadata.ItemsPerWindow < 0
				? new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferSize / 4, 4)
				: new GraphicsBuffer(GraphicsBuffer.Target.Constant, bufferSize / 4, 4);
			_graphicsBuffer.SetData(cpuGraphicsBuffer, 0, 0, cpuGraphicsBuffer.Length);
			cpuGraphicsBuffer.Dispose();
		}
		
		protected abstract JobHandle FillBuffer(int outerVoxelsCount, int indexOffset, byte* buffer, JobHandle handle);

		private void CreateBatches(int positionsCount) {
			DisposeBatches();

			var batchesCount = BatchMetadata.GetWindowsCount(positionsCount);
			_batchIDs = new NativeArray<BatchID>(batchesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			var metadataValues = new NativeArray<MetadataValue>(BatchMetadata.Length, Allocator.Temp,
				NativeArrayOptions.UninitializedMemory);
			var bufferOffset = 0;
			var batchIndex = 0;
			while (positionsCount > 0) {
				var batchOffset = 0;
				var batchPositionsCount = BatchMetadata.ItemsPerWindow > 0
					? Mathf.Min(BatchMetadata.ItemsPerWindow, positionsCount)
					: positionsCount;
				for (var j = 0; j < BatchMetadata.Length; j++) {
					metadataValues[j] = BatchMetadata.GetValue(j).GetMetadataValue(ref batchOffset, batchPositionsCount);
				}

				_batchIDs[batchIndex++] = _batchRendererGroup.AddBatch(metadataValues, _graphicsBuffer.bufferHandle,
					(uint)bufferOffset,
					BatchMetadata.ItemsPerWindow > 0 ? (uint)BatchRendererGroup.GetConstantBufferMaxWindowSize() : 0);
				bufferOffset += BatchRendererGroup.GetConstantBufferMaxWindowSize();
				positionsCount -= batchPositionsCount;
			}
			
			metadataValues.Dispose();

			if (_visibleSideVoxelsCount.IsCreated) {
				_visibleSideVoxelsCount.Dispose();
				_visibleVoxelsOffsets.Dispose();
			}
			_visibleSideVoxelsCount = new NativeArray<int>(6 * batchesCount, Allocator.Persistent);
			_visibleVoxelsOffsets = new NativeArray<int>(6 * batchesCount, Allocator.Persistent);
		}

		private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
			BatchCullingOutput cullingOutput, IntPtr userContext) {
			var offset = 0;
			var batchesCount = _batchIDs.Length;
			
			for (var i = 0; i < 6; i++) {
				_visibleVoxelsOffsets[i * batchesCount] = offset;
				offset += _quadRenderers[i].SideVoxelsIndicesLength;
			}
			// !!!!!!!
			var visibleSideVoxelsArray = FillDrawCommandJob.Malloc<int>((uint)offset);
			
			var cameraTransform = Camera.main.transform;
			var cameraPosition = cameraTransform.position;
			var cameraForward = cameraTransform.forward;
			
			
			var handle = default(JobHandle);
			var itemsPerWindow = BatchMetadata.ItemsPerWindow;
			if (itemsPerWindow < 0) itemsPerWindow = _outerVoxels.Length;
			for (var i = 0; i < 6; i++) {
				handle = JobHandle.CombineDependencies(handle,
					_quadRenderers[i].OnPerformCulling(cameraPosition, cameraForward, visibleSideVoxelsArray,
						_visibleVoxelsOffsets, _visibleSideVoxelsCount, _batchIDs.Length, itemsPerWindow));
			}
			
			var fillDrawCommandJob = new FillDrawCommandJob(cullingOutput.drawCommands, _castShadows, _batchIDs, _batchMaterialID,
				_batchMeshIDs, visibleSideVoxelsArray, _visibleVoxelsOffsets, _visibleSideVoxelsCount, itemsPerWindow, _batchIDs.Length,
				_outerVoxels.Length);

			handle = fillDrawCommandJob.Schedule(handle);
			return handle;
		}

		protected virtual void OnDestroy() {
			DisposeBatches();
			_batchRendererGroup.UnregisterMaterial(_batchMaterialID);
			for (var i = 0; i < 6; i++) {
				_batchRendererGroup.UnregisterMesh(_batchMeshIDs[i]);
			}
			_batchRendererGroup.Dispose();
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Dispose();
			}

			_visibleSideVoxelsCount.Dispose();
			_visibleVoxelsOffsets.Dispose();

			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_bonePositionsArray.Dispose();
			_boneAnimationPositionsArray.Dispose();
			_boneAnimationRotationsArray.Dispose();
			
			_shaderVoxelsArray.Dispose();

			_voxelBoxMasks.Dispose();
			
			_graphicsBuffer.Dispose();

			_batchMeshIDs.Dispose();
		}
		
		private void DisposeBatches() {
			if (_batchIDs.IsCreated) {
				for (var i = 0; i < _batchIDs.Length; i++) {
					_batchRendererGroup.RemoveBatch(_batchIDs[i]);
				}
				_batchIDs.Dispose();
			}
		}
    }
}