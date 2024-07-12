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

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    public unsafe abstract class BrgRenderer : MonoBehaviour, IVoxelRenderer {
		[SerializeField]
		protected Voxels _voxels;
		[SerializeField]
		protected Material _material;
		private readonly bool _castShadows;
		
		protected Bounds _bounds;

		protected float _voxelSize;
		protected Vector3 _startPosition;
		protected VoxelsBox _box;
		protected int _positionsCount;
		protected int _bonesCount;
		
		protected float _animationFrameRate;
		protected float _animationTime;
		protected int _animationLength;
		
		protected int _animationCurrentFrame;
		protected int _animationNextFrame;
		protected float _animationLerpRatio;
		
		protected BrgQuadRenderer[] _quadRenderers;
		
		protected NativeArray<ShaderVoxel> _shaderVoxelsArray;
		protected NativeArray<byte> _voxelBoxMasks;
		protected NativeList<int> _outerVoxels;
		
		protected NativeArray<float3> _bonePositionsArray;
		protected NativeArray<float3> _boneAnimationPositionsArray;
		protected NativeArray<float4> _boneAnimationRotationsArray;

		protected abstract BatchMetadata BatchMetadata { get; }

		private BatchRendererGroup _batchRendererGroup;
		protected GraphicsBuffer _graphicsBuffer;
		private NativeArray<BatchID> _batchIDs;
		private BatchMaterialID _batchMaterialID;
		private NativeArray<BatchMeshID> _batchMeshIDs;
		private NativeArray<int> _visibleSideVoxelsCount;

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
			
			var colorsArray = new NativeArray<byte>(_voxels.Colors, Allocator.TempJob);
			var colorsSlice = new NativeSlice<byte>(colorsArray).SliceConvert<byte3>();
			var positionsArray = new NativeArray<byte>(_voxels.Indices, Allocator.TempJob);
			var positionsSlice = new NativeSlice<byte>(positionsArray).SliceConvert<byte3>();
			var bonesArray = new NativeArray<byte>(_voxels.Bones, Allocator.TempJob);
			var bonesSlice = new NativeSlice<byte>(bonesArray).SliceConvert<byte>();

			_bonePositionsArray = _voxels.Animation.BonesPositions.Length == 0
				? new NativeArray<float3>(2, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.BonesPositions, Allocator.Persistent);
			_boneAnimationPositionsArray = _voxels.Animation.AnimationBonesPositions.Length == 0
				? new NativeArray<float3>(2, Allocator.Persistent)
				: new NativeArray<float3>(_voxels.Animation.AnimationBonesPositions, Allocator.Persistent);
			_boneAnimationRotationsArray = _voxels.Animation.AnimationBonesRotations.Length == 0
				? new NativeArray<float4>(2, Allocator.Persistent)
				: new NativeArray<float4>(_voxels.Animation.AnimationBonesRotations, Allocator.Persistent);

			_positionsCount = positionsSlice.Length;
			_shaderVoxelsArray = new NativeArray<ShaderVoxel>(_positionsCount, Allocator.Persistent);
			var boneMasks = new NativeArray<byte>(_positionsCount, Allocator.TempJob);

			_voxelBoxMasks = new NativeArray<byte>(_box.Count, Allocator.Persistent);
			var voxelBoxBones = new NativeArray<byte>(_box.Count, Allocator.TempJob);

			var setupVoxelsJob = new SetupRuntimeVoxelsJob(_box, positionsSlice, colorsSlice, bonesSlice,
				_shaderVoxelsArray, _voxelBoxMasks, voxelBoxBones);
			var maskSameBoneJob = new MaskSameBoneJob(_box, positionsSlice, voxelBoxBones, boneMasks);
			var maskVoxelSidesJob = new MaskVoxelSidesJob(_box, positionsSlice, boneMasks, _voxelBoxMasks);

			var sliceSize = _positionsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var handle = setupVoxelsJob.Schedule(_positionsCount, sliceSize);
			handle = maskSameBoneJob.Schedule(_positionsCount, sliceSize, handle);
			handle = maskVoxelSidesJob.Schedule(_positionsCount, sliceSize, handle);

			_bonesCount = _bonePositionsArray.Length;
			
			_quadRenderers = new BrgQuadRenderer[6];
			
			_visibleSideVoxelsCount = new NativeArray<int>(6, Allocator.Persistent);
			
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
				_quadRenderers[i] = new BrgQuadRenderer(i, _voxelSize, _startPosition, _bonesCount, _animationLength,
					_material, _box, _shaderVoxelsArray, _voxelBoxMasks, _bonePositionsArray,
					_boneAnimationPositionsArray, _boneAnimationRotationsArray);
			}
			
			UpdateOuterVoxels(handle);
			
			positionsArray.Dispose();
			colorsArray.Dispose();
			bonesArray.Dispose();

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
				_quadRenderers[i].UpdateOuterVoxels(outerVoxelsCount, _outerVoxels, _graphicsBuffer);
			}

			CreateBatches(outerVoxelsCount);
		}

		private void CreateBuffer(int positionsCount, JobHandle handle) {
			_graphicsBuffer?.Dispose();
			var bufferSize = BatchMetadata.GetBufferSize(positionsCount);
			var cpuGraphicsBuffer = new NativeArray<byte>(bufferSize, Allocator.Temp);
			var cpuGraphicsBufferPointer = (byte*)cpuGraphicsBuffer.GetUnsafePtr();
			var offset = 0;
			for (var i = 0; i < BatchMetadata.Length; i++) {
				var metadataValue = BatchMetadata.GetValue(i);
				metadataValue.SetValueToBuffer(cpuGraphicsBufferPointer, offset);
				offset += metadataValue.GetBufferSize(positionsCount);
			}
			FillBuffer(positionsCount, cpuGraphicsBuffer, handle).Complete();
			_graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferSize / 4, 4);
			_graphicsBuffer.SetData(cpuGraphicsBuffer, 0, 0, cpuGraphicsBuffer.Length);
			cpuGraphicsBuffer.Dispose();
		}
		
		protected abstract JobHandle FillBuffer(int outerVoxelsCount, NativeArray<byte> buffer, JobHandle handle);

		private void CreateBatches(int positionsCount) {
			DisposeBatches();
			//BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer

			var batchesCount = 1;
			_batchIDs = new NativeArray<BatchID>(batchesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			var metadataValues = new NativeArray<MetadataValue>(BatchMetadata.Length, Allocator.Temp,
				NativeArrayOptions.UninitializedMemory);
			var bufferOffset = 0;
			for (var i = 0; i < batchesCount; i++) {
				var batchOffset = 0;
				for (var j = 0; j < BatchMetadata.Length; j++) {
					metadataValues[j] = BatchMetadata.GetValue(j).GetMetadataValue(ref batchOffset, positionsCount);
				}
				_batchIDs[i] = _batchRendererGroup.AddBatch(metadataValues, _graphicsBuffer.bufferHandle,
					(uint)bufferOffset, 0);
				bufferOffset += batchOffset;
			}
			metadataValues.Dispose();
		}

		private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
			BatchCullingOutput cullingOutput, IntPtr userContext) {
			var offset = 0;
			
			var visibleSideVoxelsOffset = new NativeArray<int>(6, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			for (var i = 0; i < 6; i++) {
				visibleSideVoxelsOffset[i] = offset;
				offset += _quadRenderers[i].SideVoxelsIndicesLength;
			}
			var visibleSideVoxelsArray = FillDrawCommandJob.Malloc<int>((uint)offset);
			
			var cameraTransform = Camera.main.transform;
			var cameraPosition = cameraTransform.position;
			var cameraForward = cameraTransform.forward;
			
			
			var handle = default(JobHandle);
			for (var i = 0; i < 6; i++) {
				handle = JobHandle.CombineDependencies(handle, _quadRenderers[i].OnPerformCulling(cameraPosition, cameraForward, visibleSideVoxelsArray, visibleSideVoxelsOffset[i], _visibleSideVoxelsCount));
			}
			
			var fillDrawCommandJob = new FillDrawCommandJob(cullingOutput.drawCommands, _castShadows, _batchIDs[0], _batchMaterialID,
				_batchMeshIDs, visibleSideVoxelsArray, visibleSideVoxelsOffset, _visibleSideVoxelsCount);

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