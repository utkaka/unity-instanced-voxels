using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
	[Serializable]
	public struct VoxelsAnimation {
		[SerializeField]
		private int _bonesCount;
		[SerializeField]
		private float3[] _bonesPositions;
		[FormerlySerializedAs("_animationLength")]
		[SerializeField]
		private int _framesCount;
		[FormerlySerializedAs("_animationFrameRate")]
		[SerializeField]
		private float _frameRate;
		[SerializeField]
		private float3[] _animationBonesPositions;
		[SerializeField]
		private float4[] _animationBonesRotations;

		public int FramesCount => _framesCount;

		public float FrameRate => _frameRate;

		public int BonesCount => _bonesCount;

		public float3[] BonesPositions => _bonesPositions;

		public float3[] AnimationBonesPositions => _animationBonesPositions;

		public float4[] AnimationBonesRotations => _animationBonesRotations;

		public VoxelsAnimation(int framesCount, float frameRate, int bonesCount, float3[] bonesPositions, float3[] animationBonesPositions, float4[] animationBonesRotations) {
			_framesCount = framesCount;
			_frameRate = frameRate;
			_bonesCount = bonesCount;
			_bonesPositions = bonesPositions;
			_animationBonesPositions = animationBonesPositions;
			_animationBonesRotations = animationBonesRotations;
		}
	}
}