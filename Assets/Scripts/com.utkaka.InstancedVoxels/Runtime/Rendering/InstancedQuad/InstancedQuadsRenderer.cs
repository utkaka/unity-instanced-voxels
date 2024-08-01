using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad {
	public class InstancedQuadsRenderer : MonoBehaviour, IVoxelRenderer{
		private static readonly int ShaderVoxelSize = Shader.PropertyToID("_VoxelSize");
		private static readonly int ShaderStartPosition = Shader.PropertyToID("_StartPosition");
		private static readonly int ShaderCurrentAnimationFrame = Shader.PropertyToID("_CurrentAnimationFrame");
		private static readonly int ShaderNextAnimationFrame = Shader.PropertyToID("_NextAnimationFrame");
		private static readonly int ShaderAnimationLerpRatio = Shader.PropertyToID("_AnimationLerpRatio");
		private static readonly int ShaderAnimationFramesCount = Shader.PropertyToID("_AnimationFramesCount");
		private static readonly int ShaderBonesCount = Shader.PropertyToID("_BonesCount");
		
		private static readonly int ShaderBonePositionsBuffer = Shader.PropertyToID("bone_positions_buffer");
		private static readonly int ShaderBonePositionsAnimationBuffer = Shader.PropertyToID("bone_positions_animation_buffer");
		private static readonly int ShaderBoneRotationsAnimationBuffer = Shader.PropertyToID("bone_rotations_animation_buffer");
		
		

		[SerializeField]
		private Voxels _voxels;
		[SerializeField]
		private CullingOptions _cullingOptions;
		
		private Bounds _bounds;

		private float _voxelSize;
		private Vector3 _startPosition;
		
		private VoxelsBox _box;
		private int _positionsCount;
		private int _bonesCount;
		
		private float _animationFrameRate;
		private float _animationTime;
		private int _animationLength;
		
		private int _animationCurrentFrame;
		private int _animationNextFrame;
		private float _animationLerpRatio;

		private QuadRenderer[] _quadRenderers;
		
		private NativeArray<float3> _bonePositionsArray;
		private NativeArray<float3> _boneAnimationPositionsArray;
		private NativeArray<float4> _boneAnimationRotationsArray;
		
		private ComputeBuffer _bonePositionsBuffer;
		private ComputeBuffer _bonePositionsAnimationBuffer;
		private ComputeBuffer _boneRotationsAnimationBuffer;
		
		private NativeArray<ShaderVoxel> _shaderVoxelsArray;
		
		private NativeArray<VoxelsBounds> _visibilityBounds;
		private NativeArray<VoxelsBounds> _previousVisibilityBounds;
		
		private NativeArray<byte> _voxelBoxMasks;
		private NativeList<int> _outerVoxels;


		public void Init(Voxels voxels, CullingOptions cullingOptions) {
			_voxels = voxels;
			_cullingOptions = cullingOptions;
		}

		private void Start() {
			InitVoxels();
		}
		
		private void InitVoxels() {
			_voxelSize = _voxels.VoxelSize;
			_startPosition = _voxels.StartPosition;
			_bounds = new Bounds(Vector3.zero, 
				new Vector3(_voxels.Box.Size.x, _voxels.Box.Size.y, _voxels.Box.Size.z) * _voxels.VoxelSize);
			
			_box = new VoxelsBox(_voxels.Box.Size + new int3(2, 2, 2));
			
			_animationFrameRate = _voxels.Animation.FrameRate;
			_animationLength = _voxels.Animation.FramesCount;
			
			_quadRenderers = new QuadRenderer[6];
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i] = new QuadRenderer(i, _voxelSize, _cullingOptions);
			}
			
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

			UpdateBones(_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray);
			
			Shader.SetGlobalFloat(ShaderVoxelSize, _voxelSize);
			Shader.SetGlobalVector(ShaderStartPosition, _startPosition);
			Shader.SetGlobalFloat(ShaderAnimationFramesCount, _voxels.Animation.FramesCount);

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
			_visibilityBounds = _cullingOptions == CullingOptions.InnerSidesAndBackface || _cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate
				? new NativeArray<VoxelsBounds>(6 * _bonesCount, Allocator.Persistent)
				: new NativeArray<VoxelsBounds>(0, Allocator.Persistent);
			_previousVisibilityBounds = new NativeArray<VoxelsBounds>(_visibilityBounds.Length, Allocator.Persistent);
			
			if (_cullingOptions == CullingOptions.InnerSidesAndBackface || _cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate) {
				_outerVoxels = new NativeList<int>(_positionsCount, Allocator.Persistent);
				var cullInnerVoxelsJob = new CullInnerVoxelsJob(_box, _shaderVoxelsArray, _voxelBoxMasks, _outerVoxels);
				handle = cullInnerVoxelsJob.Schedule(_positionsCount, handle);
				handle.Complete();
				var cameraPosition = Camera.main.transform.position;
				var cameraForward = Camera.main.transform.forward;
				JobHandle visibilityBoundsHandle = default;
				for (var i = 0; i < 6; i++) {
					var calculateVisibilityBoundsJob =
						new CalculateVisibilityBoundsJob(_voxelSize, _startPosition, VoxelMeshGenerator.GetSideNormal(i), _box, cameraPosition, cameraForward,
							_voxels.Animation.FramesCount, 0, 1, 0.0f,
							_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
							new NativeSlice<VoxelsBounds>(_visibilityBounds, _bonesCount * i, _bonesCount));
					visibilityBoundsHandle = JobHandle.CombineDependencies(visibilityBoundsHandle,
						calculateVisibilityBoundsJob.Schedule(_bonesCount,
							_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle));
				}

				handle = visibilityBoundsHandle;
			}

			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].InitVoxels(_positionsCount, _box, _shaderVoxelsArray, _voxelBoxMasks,
					_visibilityBounds, _outerVoxels, handle);
			}

			positionsArray.Dispose();
			colorsArray.Dispose();
			bonesArray.Dispose();

			voxelBoxBones.Dispose();
			boneMasks.Dispose();

			if (!(_voxels.Animation.FrameRate > 0)) return;
			if (_animationLength > 0.0f) {
				StartCoroutine(AnimationUpdate());
			}

			_voxels = null;
		}

		private void LateUpdate() {
			if (_cullingOptions != CullingOptions.InnerSidesAndBackfaceUpdate) return;
			var cameraPosition = Camera.main.transform.position;
			var cameraForward = Camera.main.transform.forward;
			NativeArray<VoxelsBounds>.Copy(_visibilityBounds, _previousVisibilityBounds);
			for (var i = 0; i < 6; i++) {
				var previousVisibilityBoundsSlice = new NativeSlice<VoxelsBounds>(_previousVisibilityBounds, _bonesCount * i, _bonesCount);
				var currentVisibilityBoundsSlice = new NativeSlice<VoxelsBounds>(_visibilityBounds, _bonesCount * i, _bonesCount);
				var calculateVisibilityBoundsJob =
					new CalculateVisibilityBoundsJob(_voxelSize, _startPosition, VoxelMeshGenerator.GetSideNormal(i), _box, cameraPosition, cameraForward,
						_animationLength, _animationCurrentFrame, _animationNextFrame, _animationLerpRatio,
						_bonePositionsArray, _boneAnimationPositionsArray, _boneAnimationRotationsArray,
						currentVisibilityBoundsSlice);
				var visibilityBoundsHandle = calculateVisibilityBoundsJob.Schedule(_bonesCount,
					_bonesCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount);
				var boundsChanged = new NativeArray<bool>(1, Allocator.TempJob);
				var checkVisibilityBoundsChangedJob = new CheckVisibilityBoundsChangedJob(previousVisibilityBoundsSlice,
					currentVisibilityBoundsSlice, boundsChanged);
				visibilityBoundsHandle = checkVisibilityBoundsChangedJob.Schedule(_bonesCount, visibilityBoundsHandle);
				visibilityBoundsHandle.Complete();
				if (boundsChanged[0]) {
					_quadRenderers[i].CullingUpdate(_shaderVoxelsArray, _visibilityBounds, visibilityBoundsHandle);
				}
				boundsChanged.Dispose();
			}
		}
		
		private void Update() {
			if (_cullingOptions == CullingOptions.InnerSidesAndBackfaceUpdate) {
				for (var i = 0; i < 6; i++) {
					_quadRenderers[i].UpdateVoxels();
				}
			}
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Render(_bounds);
			}
		}

		private IEnumerator AnimationUpdate() {
			var wait = new WaitForEndOfFrame();
			while (true) {
				yield return wait;
				//_animationTime += _animationFrameRate * Time.deltaTime;
				if (_animationTime >= _animationLength) {
					_animationTime -= _animationLength;
				}
				_animationCurrentFrame = (int)_animationTime;
				_animationNextFrame = (_animationCurrentFrame + 1) % _animationLength;
				_animationLerpRatio = _animationTime - _animationCurrentFrame;
				
				Shader.SetGlobalFloat(ShaderCurrentAnimationFrame, _animationCurrentFrame);
				Shader.SetGlobalFloat(ShaderNextAnimationFrame, _animationNextFrame);
				Shader.SetGlobalFloat(ShaderAnimationLerpRatio, _animationLerpRatio);
			}
		}

		private void UpdateBones(NativeArray<float3> bonePositions, NativeArray<float3> boneAnimationPositions, NativeArray<float4> boneAnimationRotations) {
			SetGlobalBufferData(ShaderBonePositionsBuffer, ref _bonePositionsBuffer, bonePositions, 12);
			SetGlobalBufferData(ShaderBonePositionsAnimationBuffer, ref _bonePositionsAnimationBuffer, boneAnimationPositions, 12);
			SetGlobalBufferData(ShaderBoneRotationsAnimationBuffer, ref _boneRotationsAnimationBuffer, boneAnimationRotations, 16);
		}

		private void SetGlobalBufferData<T>(int nameId, ref ComputeBuffer buffer, NativeArray<T> data, int stride) where T : struct {
			buffer?.Dispose();
			buffer = new ComputeBuffer(data.Length, stride);
			buffer.SetData(data);
			Shader.SetGlobalBuffer(nameId, buffer);
		}

		private void OnDestroy() {
			for (var i = 0; i < 6; i++) {
				_quadRenderers[i].Dispose();
			}

			if (_outerVoxels.IsCreated) _outerVoxels.Dispose();
			
			_bonePositionsBuffer?.Dispose();
			_bonePositionsAnimationBuffer?.Dispose();
			_boneRotationsAnimationBuffer?.Dispose();
			
			_bonePositionsArray.Dispose();
			_boneAnimationPositionsArray.Dispose();
			_boneAnimationRotationsArray.Dispose();
			
			_visibilityBounds.Dispose();
            _previousVisibilityBounds.Dispose();
			_shaderVoxelsArray.Dispose();

			_voxelBoxMasks.Dispose();
		}
	}
}