using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects {
	public class GameObjectBone : MonoBehaviour {
		private Vector3 _initialPosition;
		private int _bone;
		private VoxelsAnimation _voxelsAnimation;

		public void SetupBone(int bone, VoxelsAnimation voxelsAnimation) {
			_bone = bone;
			_initialPosition = transform.position;
			_voxelsAnimation = voxelsAnimation;
		}

		public void UpdateAnimation(int frame, int nextFrame, float lerp) {
			frame += _bone * _voxelsAnimation.FramesCount;
			nextFrame += _bone * _voxelsAnimation.FramesCount;
			var animationPosition = math.lerp(_voxelsAnimation.AnimationBonesPositions[frame],
				_voxelsAnimation.AnimationBonesPositions[nextFrame], lerp);
			var animationRotation = math.lerp(_voxelsAnimation.AnimationBonesRotations[frame],
				_voxelsAnimation.AnimationBonesRotations[nextFrame], lerp);
			transform.SetPositionAndRotation(_initialPosition + (Vector3) animationPosition,
				(quaternion) animationRotation);
		}
	}
}