using System;
using System.Collections;
using InstancedVoxels.VoxelData;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Demo {
	public class VoxelizationDemo : MonoBehaviour {
		[SerializeField]
		private Voxels[] _voxels;
		[SerializeField]
		private GameObject _prefab;
		[SerializeField]
		private Animation _animation;
	
		private Transform[] _bones;

		private AnimationState _animationState;

		private void Start() {
			_animationState = _animation[_animation.clip.name];
			StartCoroutine(AnimationCoroutine());
		}

		private IEnumerator AnimationCoroutine() {
			var m = 0;
			while (true) {
				if (m == _voxels.Length) m = 0;
				var box = _voxels[m].Box;
				var colors = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Colors, Allocator.Temp)).SliceConvert<VoxelColor32>();
				var indices = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Indices, Allocator.Temp)).SliceConvert<int>();
				var bones = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Bones, Allocator.Temp)).SliceConvert<int>();
				var voxelSize = _voxels[m].VoxelSize;
				var startPosition = _voxels[m].StartPosition;

				_bones = new Transform[_voxels[m].Animation.BonesCount];
				for (var i = 0; i < _bones.Length; i++) {
					var bone = new GameObject($"Bone_{i}", typeof(DemoBone)).transform;
					bone.SetParent(transform);
					bone.position = _voxels[m].Animation.BonesPositions[i];
					bone.GetComponent<DemoBone>().SetupBone(i, _voxels[m].Animation, _animationState);
					_bones[i] = bone;
				}

				var voxelsCount = indices.Length;
				for (var i = 0; i < voxelsCount; i++) {
					var index = indices[i];
					var position = box.GetVoxelPosition(index);
					if (position.x > box.Size.x / 2) continue;
					var color = colors[i];
					var bone = bones[i];
					color = new VoxelColor32((byte) (bone * 32), (byte) (bone * 16), (byte) (bone * 8));
					var voxel = new GameObject($"voxel-{position}", typeof(DemoVoxel)).GetComponent<DemoVoxel>();
					var voxelTransform = voxel.transform;
					voxelTransform.position = startPosition + (Vector3)((float3)position * voxelSize) - Vector3.one * voxelSize * 0.5f;
					voxelTransform.SetParent(_bones[bone], true);
					voxel.SetupVoxel(_prefab, color, voxelSize);
				}
				m++;
				yield return new WaitForSeconds(6.5f);
				foreach (var bone in _bones) {
					Destroy(bone.gameObject);
				}
			}
		}
	}
}