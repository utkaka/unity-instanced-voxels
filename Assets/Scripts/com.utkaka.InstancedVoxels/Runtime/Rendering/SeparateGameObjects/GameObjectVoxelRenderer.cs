using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects {
	public class GameObjectVoxelRenderer : MonoBehaviour, IVoxelRenderer {
		[SerializeField]
		private Voxels _voxels;
		private GameObjectBone[] _bones;
		private float _animationLength;
		private float _animationTime;

		public void Init(Voxels voxels) {
			_voxels = voxels;
		}

		private void Start() {
			InitVoxels();
		}

		private void InitVoxels() {
			var cubeMesh = VoxelMeshGenerator.GetCubeMesh(_voxels.VoxelSize);
			var prefab = new GameObject("Voxel", typeof(MeshFilter), typeof(MeshRenderer));
			prefab.GetComponent<MeshFilter>().sharedMesh = cubeMesh;
			prefab.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
			
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
				var voxel = Instantiate(prefab, transform, false).GetComponent<MeshRenderer>();
				voxel.gameObject.name = $"voxel-{position}";
				var voxelTransform = voxel.transform;
				voxelTransform.position = startPosition + position * voxelSize;
				voxelTransform.SetParent(_bones[bone].transform, true);
				voxel.material.color = new Color(color.x / 255.0f, color.y / 255.0f, color.z / 255.0f);
			}
			Destroy(prefab);

			colorsArray.Dispose();
			positionsArray.Dispose();
			bonesArray.Dispose();

			if (!(_voxels.Animation.FrameRate > 0)) return;
			_animationLength = _voxels.Animation.FramesCount / _voxels.Animation.FrameRate;
			if (_animationLength > 0.0f) {
				StartCoroutine(AnimationUpdate());
			}
		}

		private IEnumerator AnimationUpdate() {
			var wait = new WaitForEndOfFrame();
			while (true) {
				yield return wait;
				_animationTime += Time.deltaTime;
				if (_animationTime >= _animationLength) {
					_animationTime -= _animationLength;
				}
				var currentFrame = _voxels.Animation.FramesCount * (_animationTime / _animationLength);
				var lerpRatio = currentFrame - Mathf.Floor(currentFrame);
				var nextFrame = (currentFrame + 1.0f) % _voxels.Animation.FramesCount;
				for (var i = 0; i < _bones.Length; i++) {
					_bones[i].UpdateAnimation((int)currentFrame, (int)nextFrame, lerpRatio);
				}	
			}
		}
	}
}