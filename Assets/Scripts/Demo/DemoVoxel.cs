using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Demo {
	public class DemoVoxel : MonoBehaviour {
		public void SetupVoxel(GameObject prefab, byte3 color, float voxelSize) {
			StartCoroutine(AnimationCoroutine(prefab, color, voxelSize));
		}

		private IEnumerator AnimationCoroutine(GameObject prefab, byte3 color, float voxelSize) {
			var delay = Random.Range(0.2f, 2.0f);
			yield return new WaitForSeconds(delay);
			var cube = Instantiate(prefab, transform, false);
			var meshRenderer = cube.GetComponent<MeshRenderer>();
			meshRenderer.material.color = new Color(color.x / 255.0f, color.y / 255.0f, color.z / 255.0f);
			transform.localScale = voxelSize * Vector3.one;
			yield return new WaitForSeconds(Random.Range(3.0f, 4.0f));
			Destroy(gameObject);
		}
	}
}