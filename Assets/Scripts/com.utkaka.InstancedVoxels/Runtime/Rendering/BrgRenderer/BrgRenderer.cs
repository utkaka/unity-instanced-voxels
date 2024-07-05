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
		
		private BatchRendererGroup _batchRendererGroup;
		protected GraphicsBuffer _graphicsBuffer;
		private BatchID _batchID;
		private BatchMaterialID _batchMaterialID;
		private NativeArray<BatchMeshID> _batchMeshIDs;

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
			
			UpdateBuffer(outerVoxelsCount, handle);
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].UpdateOuterVoxels(outerVoxelsCount, _outerVoxels, _graphicsBuffer);
			}
			
			_batchRendererGroup.RemoveBatch(_batchID);
			var batchMetadata = CreateMetadata(outerVoxelsCount);
			_batchID = _batchRendererGroup.AddBatch(batchMetadata, _graphicsBuffer.bufferHandle,
				0, 0);
			batchMetadata.Dispose();
		}
		
		protected abstract NativeArray<MetadataValue> CreateMetadata(int positionsCount);

		protected abstract void UpdateBuffer(int outerVoxelsCount, JobHandle handle);

		private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
			BatchCullingOutput cullingOutput, IntPtr userContext) {
			var offset = 0;
			var visibleSideVoxelsCount = new NativeArray<int>(6, Allocator.TempJob);
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
				handle = JobHandle.CombineDependencies(handle, _quadRenderers[i].OnPerformCulling(cameraPosition, cameraForward, visibleSideVoxelsArray, visibleSideVoxelsOffset[i], visibleSideVoxelsCount));
			}
			
			var fillDrawCommandJob = new FillDrawCommandJob(cullingOutput.drawCommands, _castShadows, _batchID, _batchMaterialID,
				_batchMeshIDs, visibleSideVoxelsArray, visibleSideVoxelsOffset, visibleSideVoxelsCount);

			handle = fillDrawCommandJob.Schedule(handle);
			return handle;
		}

		protected static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance) {
			const uint kIsPerInstanceBit = 0x80000000;
			return new MetadataValue
			{
				NameID = nameID,
				Value = (uint)gpuOffset | (isPerInstance ? (kIsPerInstanceBit) : 0),
			};
		}

		protected virtual void OnDestroy() {
			
			_batchRendererGroup.RemoveBatch(_batchID);
			_batchRendererGroup.UnregisterMaterial(_batchMaterialID);
			for (var i = 0; i < 6; i++) {
				_batchRendererGroup.UnregisterMesh(_batchMeshIDs[i]);
			}
			_batchRendererGroup.Dispose();
			
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Dispose();
			}

			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_bonePositionsArray.Dispose();
			_boneAnimationPositionsArray.Dispose();
			_boneAnimationRotationsArray.Dispose();
			
			_shaderVoxelsArray.Dispose();

			_voxelBoxMasks.Dispose();
			
			_graphicsBuffer.Dispose();

			_batchMeshIDs.Dispose();
			
			
		}
    }
}