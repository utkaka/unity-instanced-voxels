using System.Collections;
using UnityEngine;

namespace Demo {
	public class DemoVoxel : MonoBehaviour{
		public void SetupVoxel(GameObject prefab, Color color, float voxelSize) {
			StartCoroutine(AnimationCoroutine(prefab, color, voxelSize));
		}

		private IEnumerator AnimationCoroutine(GameObject prefab, Color color, float voxelSize) {
			var delay = Random.Range(0.2f, 2.0f);
			yield return new WaitForSeconds(delay);
			var cube = Instantiate(prefab, transform, false);
			transform.localScale = voxelSize * Vector3.one;
			var meshRenderer = cube.GetComponent<MeshRenderer>();
			meshRenderer.material.color = color;
			yield return new WaitForSeconds(Random.Range(3.0f, 4.0f));
			Destroy(gameObject);
		}
	}
}