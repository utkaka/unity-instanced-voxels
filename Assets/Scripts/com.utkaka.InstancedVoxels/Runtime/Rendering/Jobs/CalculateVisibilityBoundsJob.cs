using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct CalculateVisibilityBoundsJob : IJobParallelFor {
		private readonly float _voxelSize;
		private readonly float3 _startPosition;
		private readonly float3 _sideNormal;
		private readonly VoxelsBox _voxelsBox;
		private readonly float3 _cameraPosition;
		private readonly float3 _cameraForward;
		private readonly int _framesCount;
		private readonly int _currentAnimationFrame;
		private readonly int _nextAnimationFrame;
		private readonly float _frameTransitionRatio;
		[ReadOnly]
		private NativeArray<float3> _bonesPositions;
		[ReadOnly]
		private NativeArray<float3> _bonesAnimationPositions;
		[ReadOnly]
		private NativeArray<float4> _bonesAnimationRotations;
		[WriteOnly, NativeDisableContainerSafetyRestriction]
		private NativeSlice<VoxelsBounds> _visibilityBounds;

		public CalculateVisibilityBoundsJob(float voxelSize, float3 startPosition, float3 sideNormal,
			VoxelsBox voxelsBox, float3 cameraPosition, float3 cameraForward, int framesCount, int currentAnimationFrame,
			int nextAnimationFrame, float frameTransitionRatio, NativeArray<float3> bonesPositions,
			NativeArray<float3> bonesAnimationPositions, NativeArray<float4> bonesAnimationRotations,
			NativeSlice<VoxelsBounds> visibilityBounds) {
			_voxelSize = voxelSize;
			_startPosition = startPosition;
			_sideNormal = sideNormal;
			_voxelsBox = voxelsBox;
			_cameraPosition = cameraPosition;
			_cameraForward = cameraForward;
			_framesCount = framesCount;
			_currentAnimationFrame = currentAnimationFrame;
			_nextAnimationFrame = nextAnimationFrame;
			_frameTransitionRatio = frameTransitionRatio;
			_bonesPositions = bonesPositions;
			_bonesAnimationPositions = bonesAnimationPositions;
			_bonesAnimationRotations = bonesAnimationRotations;
			_visibilityBounds = visibilityBounds;
		}

		public void Execute(int index) {
			var animationIndex = _framesCount * index;
			var bonePosition = _bonesPositions[index];
			var animationRotation = math.lerp(_bonesAnimationRotations[animationIndex + _currentAnimationFrame], _bonesAnimationRotations[animationIndex + _nextAnimationFrame],
				_frameTransitionRatio);
			var animationPosition = math.lerp(_bonesAnimationPositions[animationIndex + _currentAnimationFrame],
				_bonesAnimationPositions[animationIndex + _nextAnimationFrame],
				_frameTransitionRatio);
			
			var quadBounds1 = GetQuadBounds(new byte3(0, 0, 0), byte3.right(), byte3.up(), _voxelsBox.Size.x,
				_voxelsBox.Size.y, index, bonePosition, animationPosition, animationRotation);
			var quadBounds2 = GetQuadBounds(new byte3(0, 0, 0), byte3.forward(), byte3.up(), _voxelsBox.Size.z,
				_voxelsBox.Size.y, index, bonePosition, animationPosition, animationRotation);
			var quadBounds3 = GetQuadBounds(new byte3(0, 0, (byte)_voxelsBox.Size.z), byte3.right(), byte3.up(), _voxelsBox.Size.z,
				_voxelsBox.Size.y, index, bonePosition, animationPosition, animationRotation);
			var quadBounds4 = GetQuadBounds(new byte3((byte)_voxelsBox.Size.x, 0, 0), byte3.forward(), byte3.up(), _voxelsBox.Size.z,
				_voxelsBox.Size.y, index, bonePosition, animationPosition, animationRotation);

			var minPoint = new int3(
				math.min(quadBounds1.x, quadBounds3.x),
				math.min(quadBounds1.z, math.min(quadBounds2.z, math.min(quadBounds3.z, quadBounds4.z))),
				math.min(quadBounds2.x, quadBounds4.x));
			
			var maxPoint = new int3(
				math.max(quadBounds1.y, quadBounds3.y),
				math.max(quadBounds1.w, math.max(quadBounds2.w, math.max(quadBounds3.w, quadBounds4.w))),
				math.max(quadBounds2.y, quadBounds4.y));
			
			_visibilityBounds[index] = new VoxelsBounds(minPoint, maxPoint);
		}

		private int4 GetQuadBounds(byte3 start, byte3 axis1, byte3 axis2, int boxAxisSize1, int boxAxisSize2, int boneIndex, float3 bonePosition,
			float3 animationPosition, float4 animationRotation) {
			var squareSize = math.min(boxAxisSize1, boxAxisSize2);
			var squareBounds = GetAxisBounds(start, axis1 + axis2, squareSize, bonePosition,
				animationPosition, animationRotation);
			var axis1Bounds1 = GetAxisBounds(start + axis2 * squareBounds.x, axis1, boxAxisSize1, bonePosition,
				animationPosition, animationRotation);
			var axis1Bounds2 = GetAxisBounds(start + axis2 * squareBounds.y, axis1, boxAxisSize1, bonePosition,
				animationPosition, animationRotation);
			var axis2Bounds1 = GetAxisBounds(start + axis1 * squareBounds.x, axis2, boxAxisSize2, bonePosition,
				animationPosition, animationRotation);
			var axis2Bounds2 = GetAxisBounds(start + axis1 * squareBounds.y, axis2, boxAxisSize2, bonePosition,
				animationPosition, animationRotation);
			return new int4(math.min(axis1Bounds1.x, axis1Bounds2.x), math.max(axis1Bounds1.y, axis1Bounds2.y),
				math.min(axis2Bounds1.x, axis2Bounds2.x), math.max(axis2Bounds1.y, axis2Bounds2.y));
		}

		private int2 GetAxisBounds(byte3 start, byte3 axis, int boxAxisSize, float3 bonePosition, float3 animationPosition, float4 animationRotation) {
			var left = 0;
			var right = boxAxisSize + 1;
			if (IsVisible(start, bonePosition, animationPosition, animationRotation)) {
				//rightmost visible
				while (left < right) {
					var middle = (left + right) / 2;
					if (!IsVisible(start + axis * middle, bonePosition, animationPosition, animationRotation)) {
						right = middle;
					} else {
						left = middle + 1;
					}
				}
				return new int2(0, right - 1);
			}
			//leftmost visible
			while (left < right) {
				var middle = (left + right) / 2;
				if (!IsVisible(start + axis * middle, bonePosition, animationPosition, animationRotation)) {
					left = middle + 1;
				} else {
					right = middle;
				}
			}
			return new int2(left, boxAxisSize);
		}
		
		private bool IsVisible(byte3 voxel, float3 bonePosition, float3 animationPosition, float4 animationRotation) {
			var voxelPosition = new float3(voxel.x, voxel.y, voxel.z) * _voxelSize + _startPosition;
			var quaternion = new quaternion(animationRotation);
			var offsetPoint = voxelPosition - bonePosition;
			var rotatedPoint = math.mul(quaternion, offsetPoint) + bonePosition;
			voxelPosition = rotatedPoint + animationPosition;
			var cameraToVoxel = voxelPosition - _cameraPosition;
			return math.dot(_cameraForward, cameraToVoxel) >= 0 && math.dot(math.mul(quaternion, _sideNormal), cameraToVoxel)  <= 0.0f;
		}
	}
}