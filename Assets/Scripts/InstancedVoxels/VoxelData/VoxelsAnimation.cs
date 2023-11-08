using System;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.VoxelData {
	[Serializable]
	public struct VoxelsAnimation {
		[SerializeField]
		private int _bonesCount;
		[SerializeField]
		private float3[] _bonesPositions;
		[SerializeField]
		private int _animationLength;
		[SerializeField]
		private float _animationFrameRate;
		[SerializeField]
		private float3[] _animationBonesPositions;
		[SerializeField]
		private float4[] _animationBonesRotations;

		public int AnimationLength => _animationLength;

		public float AnimationFrameRate => _animationFrameRate;

		public int BonesCount => _bonesCount;

		public float3[] BonesPositions => _bonesPositions;

		public float3[] AnimationBonesPositions => _animationBonesPositions;

		public float4[] AnimationBonesRotations => _animationBonesRotations;

		public VoxelsAnimation(int animationLength, float animationFrameRate, int bonesCount, float3[] bonesPositions, float3[] animationBonesPositions, float4[] animationBonesRotations) {
			_animationLength = animationLength;
			_animationFrameRate = animationFrameRate;
			_bonesCount = bonesCount;
			_bonesPositions = bonesPositions;
			_animationBonesPositions = animationBonesPositions;
			_animationBonesRotations = animationBonesRotations;
		}
	}
}