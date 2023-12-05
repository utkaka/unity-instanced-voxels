using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects {
	public class GameObjectVoxelRenderer : MonoBehaviour{
		[SerializeField]
		private Voxels _voxels;
		[SerializeField]
		private GameObject _prefab;
		private GameObjectBone[] _bones;
		private float _animationLength;
		private float _animationTime;

		public void Init(Voxels voxels, GameObject prefab) {
			_voxels = voxels;
			_prefab = prefab;
		}

		private void Start() {
			InitVoxels();
		}

		private void InitVoxels() {
			var colorsArray = new NativeArray<byte>(_voxels.Colors, Allocator.Temp);
			var colorsSlice = new NativeSlice<byte>(colorsArray).SliceConvert<byte3>();
			var positionsArray = new NativeArray<byte>(_voxels.Indices, Allocator.Temp);
			var positionsSlice = new NativeSlice<byte>(positionsArray).SliceConvert<byte3>();
			var bonesArray = new NativeArray<byte>(_voxels.Bones, Allocator.Temp);
			var bonesSlice = new NativeSlice<byte>(bonesArray).SliceConvert<byte>();
			var voxelSize = _voxels.VoxelSize;
			var startPosition = _voxels.StartPosition;
		
			_bones = new GameObjectBone[_voxels.Animation.BonesCount];
			for (var i = 0; i < _bones.Length; i++) {
				var boneTransform = new GameObject($"Bone_{i}", typeof(GameObjectBone)).transform;
				boneTransform.SetParent(transform);
				boneTransform.position = _voxels.Animation.BonesPositions[i];
				var bone = boneTransform.GetComponent<GameObjectBone>();
				bone.SetupBone(i, _voxels.Animation);
				_bones[i] = bone;
			}
			var voxelsCount = positionsSlice.Length;
			for (var i = 0; i < voxelsCount; i++) {
				var bytePosition = positionsSlice[i];
				var position = new Vector3(bytePosition.x, bytePosition.y, bytePosition.z);
				var color = colorsSlice[i];
				var bone = bonesSlice[i];
				var voxel = new GameObject($"voxel-{position}", typeof(GameObjectVoxel)).GetComponent<GameObjectVoxel>();
				var voxelTransform = voxel.transform;
				voxelTransform.position = startPosition + position * voxelSize - Vector3.one * (voxelSize * 0.5f);
				voxelTransform.SetParent(_bones[bone].transform, true);
				voxel.SetupVoxel(_prefab, color, (float)bonesSlice[i] / _bones.Length, voxelSize);
			}

			colorsArray.Dispose();
			positionsArray.Dispose();
			bonesArray.Dispose();

			_animationLength = _voxels.Animation.FramesCount / _voxels.Animation.FrameRate;
		}

		private void Update() {
			_animationTime += Time.deltaTime;
			if (_animationTime >= _animationLength) {
				_animationTime -= _animationLength;
			}
			var currentFrame = (_animationTime / _animationLength) * _voxels.Animation.FramesCount;
			var lerpRatio = currentFrame - Mathf.Floor(currentFrame);
			var nextFrame = (currentFrame + 1.0f) % _voxels.Animation.FramesCount;
			for (var i = 0; i < _bones.Length; i++) {
				_bones[i].UpdateAnimation((int)currentFrame, (int)nextFrame, lerpRatio);
			}
		}
	}
}