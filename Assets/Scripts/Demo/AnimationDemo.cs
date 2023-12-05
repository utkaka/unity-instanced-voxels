using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using UnityEngine;

namespace Demo {
	public class AnimationDemo : MonoBehaviour{
		[SerializeField]
		private Voxels _voxels;
		[SerializeField]
		private GameObject _prefab;
		[SerializeField]
		private Animation _animation;

		private AnimationDemoVoxel[] _demoVoxels;
		private Transform[] _bones;

		private AnimationState _animationState;
		private NativeArray<byte> _positions;

		private void Start() {
			_animationState = _animation[_animation.clip.name];
			_animation.Stop();
			StartCoroutine(AnimationCoroutine());
		}

		private IEnumerator AnimationCoroutine() {
			var box = _voxels.Box;
			var colors = new NativeSlice<byte>(new NativeArray<byte>(_voxels.Colors, Allocator.Temp)).SliceConvert<byte3>();
			_positions = new NativeArray<byte>(_voxels.Indices, Allocator.Persistent);
			var positionsSlice = new NativeSlice<byte>(_positions).SliceConvert<byte3>();
			var bones = new NativeSlice<byte>(new NativeArray<byte>(_voxels.Bones, Allocator.Temp)).SliceConvert<byte>();
			var voxelSize = _voxels.VoxelSize;
			var startPosition = _voxels.StartPosition;
		
			_bones = new Transform[_voxels.Animation.BonesCount];
			for (var i = 0; i < _bones.Length; i++) {
				var bone = new GameObject($"Bone_{i}", typeof(DemoBone)).transform;
				bone.SetParent(transform);
				bone.position = _voxels.Animation.BonesPositions[i];
				bone.GetComponent<DemoBone>().SetupBone(i, _voxels.Animation, _animationState);
				_bones[i] = bone;
			}

			var voxelsCount = positionsSlice.Length;
			_demoVoxels = new AnimationDemoVoxel[voxelsCount];
			for (var i = 0; i < voxelsCount; i++) {
				var bytePosition = positionsSlice[i];
				var position = new Vector3(bytePosition.x, bytePosition.y, bytePosition.z);
				var color = colors[i];
				var bone = bones[i];
				var voxel = new GameObject($"voxel-{position}", typeof(AnimationDemoVoxel)).GetComponent<AnimationDemoVoxel>();
				var voxelTransform = voxel.transform;
				voxelTransform.position = startPosition + position * voxelSize - Vector3.one * (voxelSize * 0.5f);
				voxelTransform.SetParent(_bones[bone], true);
				voxel.SetupVoxel(_prefab, color, (float)bones[i] / _bones.Length, voxelSize);
				_demoVoxels[i] = voxel;
			}
			yield return new WaitForSeconds(6.5f);
			_animation.GetComponentInChildren<SkinnedMeshRenderer>().gameObject.SetActive(false);
			for (var i = 0; i < voxelsCount; i++) {
				var bytePosition = positionsSlice[i];
				var position = new Vector3(bytePosition.x, bytePosition.y, bytePosition.z);
				if (position.x < box.Size.x / 2) continue;
				_demoVoxels[i].DelayedActivation(false);
			}
			yield return new WaitForSeconds(1.5f);
			_animation.Play();
			yield return new WaitForSeconds(2.5f);
			for (var i = 0; i < voxelsCount; i++) {
				//var index = indicesSlice[i];
				//var position = box.GetVoxelPosition(index);
				//if (position.x >= box.Size.x / 2) continue;
				_demoVoxels[i].DelayedColor();
			}
			yield return new WaitForSeconds(1.5f);
			for (var i = 0; i < voxelsCount; i++) {
				var bytePosition = positionsSlice[i];
				var position = new Vector3(bytePosition.x, bytePosition.y, bytePosition.z);
				if (position.x < box.Size.x / 2) continue;
				_demoVoxels[i].DelayedActivation(true);
			}
			/*for (var i = 0; i < voxelsCount; i++) {
				var bone = (byte)( * 0x255);
				_demoVoxels[i].SetColor(new VoxelColor32(bone, bone, bone));
			}
			foreach (var bone in _bones) {
				Destroy(bone.gameObject);
			}*/
		}

		private void OnDestroy() {
			_positions.Dispose();
		}
	}
}