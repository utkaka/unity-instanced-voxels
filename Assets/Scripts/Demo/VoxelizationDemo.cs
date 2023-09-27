using System.Collections;
using InstancedVoxels;
using Unity.Mathematics;
using UnityEngine;

namespace Demo {
	public class VoxelizationDemo : MonoBehaviour {
		[SerializeField]
		private ScriptableVoxels[] _voxels;
		[SerializeField]
		private GameObject _prefab;

		private void Start() {
			StartCoroutine(AnimationCoroutine());
		}

		private IEnumerator AnimationCoroutine() {
			var m = 0;
			while (true) {
				if (m == _voxels.Length) m = 0;
				var boxSize = _voxels[m].BoxSize;
				var voxels = _voxels[m].Colors;
				var voxelSize = _voxels[m].VoxelSize;
				var startPosition = _voxels[m].StartPosition;
				var boxSizeYbyZ = boxSize.y * boxSize.z;
				for (var i = 0; i < boxSize.x; i++) {
					for (var j = 0; j < boxSize.y; j++) {
						for (var k = 0; k < boxSize.z; k++) {
							var color = voxels[i * boxSizeYbyZ + j * boxSize.z + k];
							if (color == Color.clear) continue;
							var voxel = new GameObject($"voxel-{i}-{j}-{k}", typeof(DemoVoxel)).GetComponent<DemoVoxel>();
							var voxelTransform = voxel.transform;
							voxelTransform.SetParent(transform);
							voxelTransform.position = startPosition + (Vector3)(new float3(i, j, k) * voxelSize) + Vector3.one * voxelSize * 0.5f;
							voxel.SetupVoxel(_prefab, color, voxelSize);
						}
					}
				}
				m++;
				yield return new WaitForSeconds(6.5f);
			}
		}
	}
}