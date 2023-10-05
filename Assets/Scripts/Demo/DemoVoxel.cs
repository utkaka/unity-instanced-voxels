using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Demo {
	public class DemoVoxel : MonoBehaviour{
		public void SetupVoxel(GameObject prefab, float3 color, float voxelSize) {
			StartCoroutine(AnimationCoroutine(prefab, color, voxelSize));
		}

		private IEnumerator AnimationCoroutine(GameObject prefab, float3 color, float voxelSize) {
			var delay = Random.Range(0.2f, 2.0f);
			yield return new WaitForSeconds(delay);
			var cube = Instantiate(prefab, transform, false);
			transform.localScale = voxelSize * Vector3.one;
			var meshRenderer = cube.GetComponent<MeshRenderer>();
			meshRenderer.material.color = new Color(color.x, color.y, color.z);
			yield return new WaitForSeconds(Random.Range(3.0f, 4.0f));
			Destroy(gameObject);
		}
	}
}