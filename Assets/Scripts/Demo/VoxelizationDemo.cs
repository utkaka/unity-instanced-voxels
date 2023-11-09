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
	
		private AnimationState _animationState;

		private void Start() {
			StartCoroutine(AnimationCoroutine());
		}

		private IEnumerator AnimationCoroutine() {
			var m = 0;
			while (true) {
				if (m == _voxels.Length) m = 0;
				var colors = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Colors, Allocator.Temp)).SliceConvert<byte3>();
				var positions = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Indices, Allocator.Temp)).SliceConvert<byte3>();
				var voxelSize = _voxels[m].VoxelSize;
				var startPosition = _voxels[m].StartPosition;

				var voxelsCount = positions.Length;
				for (var i = 0; i < voxelsCount; i++) {
					var bytePosition = positions[i];
					var position = new Vector3(bytePosition.x, bytePosition.y, bytePosition.z);
					var color = colors[i];
					var voxel = new GameObject($"voxel-{position}", typeof(DemoVoxel)).GetComponent<DemoVoxel>();
					var voxelTransform = voxel.transform;
					voxelTransform.position = startPosition + position * voxelSize - Vector3.one * (voxelSize * 0.5f);
					voxelTransform.SetParent(transform, true);
					voxel.SetupVoxel(_prefab, color, voxelSize);
				}
				m++;
				yield return new WaitForSeconds(6.5f);
			}
		}
	}
}