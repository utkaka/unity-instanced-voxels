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
				var box = _voxels[m].Box;
				var colors = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Colors, Allocator.Temp)).SliceConvert<VoxelColor32>();
				var indices = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Indices, Allocator.Temp)).SliceConvert<int>();
				var voxelSize = _voxels[m].VoxelSize;
				var startPosition = _voxels[m].StartPosition;

				var voxelsCount = indices.Length;
				for (var i = 0; i < voxelsCount; i++) {
					var index = indices[i];
					var position = box.GetVoxelPosition(index);
					var color = colors[i];
					var voxel = new GameObject($"voxel-{position}", typeof(DemoVoxel)).GetComponent<DemoVoxel>();
					var voxelTransform = voxel.transform;
					voxelTransform.position = startPosition + (Vector3)((float3)position * voxelSize) - Vector3.one * voxelSize * 0.5f;
					voxelTransform.SetParent(transform, true);
					voxel.SetupVoxel(_prefab, color, voxelSize);
				}
				m++;
				yield return new WaitForSeconds(6.5f);
			}
		}
	}
}