using InstancedVoxels.VoxelData;
using Unity.Mathematics;
using UnityEngine;

namespace Demo {
	public class DemoBone : MonoBehaviour {
		private Vector3 _initialPosition;
		private VoxelsAnimation _animation;
		private int _bone;
		private AnimationState _animationState;

		public void SetupBone(int bone, VoxelsAnimation voxelsAnimation, AnimationState animationState) {
			_animationState = animationState;
			_animation = voxelsAnimation;
			_bone = bone;
			_initialPosition = transform.position;
		}
		
		private void Update() {
			var animationIndex = _animationState.normalizedTime % 1.0f * _animation.AnimationLength;
			var lerpRatio = animationIndex - Mathf.Floor(animationIndex);
			var nextAnimationIndex = (animationIndex + 1) % _animation.AnimationLength + _bone * _animation.AnimationLength;
			animationIndex += _bone * _animation.AnimationLength;
			var animationPosition = math.lerp(_animation.AnimationBonesPositions[(int) animationIndex],
				_animation.AnimationBonesPositions[(int) nextAnimationIndex], lerpRatio);
			var animationRotation = math.lerp(_animation.AnimationBonesRotations[(int) animationIndex],
				_animation.AnimationBonesRotations[(int) nextAnimationIndex], lerpRatio);

			transform.SetPositionAndRotation(_initialPosition + (Vector3)animationPosition, (quaternion)animationRotation);
		}
	}
}