using System.Collections;
using InstancedVoxels;
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

		private void Start() {
			StartCoroutine(AnimationCoroutine());
		}

		private IEnumerator AnimationCoroutine() {
			var m = 0;
			while (true) {
				if (m == _voxels.Length) m = 0;
				var boxSize = _voxels[m].Box.Size;
				var colors = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Colors, Allocator.Temp)).SliceConvert<VoxelColor32>();
				//var indices = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Indices, Allocator.Temp)).SliceConvert<int>();
				var bones = new NativeSlice<byte>(new NativeArray<byte>(_voxels[m].Bones, Allocator.Temp)).SliceConvert<int>();
				var voxelSize = _voxels[m].VoxelSize;
				var startPosition = _voxels[m].StartPosition;
				var boxIndex = 0;
				//var colorIndex = 0;
				for (var i = 0; i < boxSize.x; i++) {
					for (var j = 0; j < boxSize.y; j++) {
						for (var k = 0; k < boxSize.z; k++) {
							/*if (colorIndex >= indices.Length) {
								break;
							}
							if (indices[colorIndex] != boxIndex) {
								boxIndex++;
								continue;
							}*/
							
							if (i > boxSize.x / 2) {
								boxIndex++;
								continue;
							}
							
							var color = colors[boxIndex];
							//TODO: temp solution, because of vertices compression is turned off
							if (color is {r: 0, g: 0, b: 0}) {
								boxIndex++;
								continue;
							}
							var bone = bones[boxIndex];
							//var color = new float3((bone % 16) / 16.0f, (bone % 8) / 8.0f, (bone % 4) / 4.0f)
							var voxel = new GameObject($"voxel-{i}-{j}-{k}", typeof(DemoVoxel)).GetComponent<DemoVoxel>();
							var voxelTransform = voxel.transform;
							voxelTransform.SetParent(transform);
							voxelTransform.position = startPosition + (Vector3)(new float3(i - 1, j - 1, k - 1) * voxelSize) + Vector3.one * voxelSize * 0.5f;
							
							voxel.SetupVoxel(_prefab, color, voxelSize);
							boxIndex++;
						}
					}
				}
				m++;
				yield return new WaitForSeconds(6.5f);
			}
		}
	}
}