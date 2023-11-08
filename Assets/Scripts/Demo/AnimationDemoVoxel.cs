using System.Collections;
using InstancedVoxels.VoxelData;
using UnityEngine;

namespace Demo {
	public class AnimationDemoVoxel : MonoBehaviour {
		private VoxelColor32 _color;
		private GameObject _cube;
		
		public void SetupVoxel(GameObject prefab, VoxelColor32 color, float bone, float voxelSize) {
			_color = color;
			StartCoroutine(AnimationCoroutine(prefab, bone, voxelSize));
		}

		private void SetColor(VoxelColor32 color) {
			var meshRenderer = _cube.GetComponent<MeshRenderer>();
			meshRenderer.material.color = new Color(color.r / 255.0f, color.g / 255.0f, color.b / 255.0f);
		}
		
		public void DelayedColor() {
			StartCoroutine(ColorCoroutine());
		}

		public void DelayedActivation(bool active) {
			StartCoroutine(ActivationCoroutine(active));
		}

		private IEnumerator AnimationCoroutine(GameObject prefab, float bone, float voxelSize) {
			var delay = Random.Range(0.2f, 2.0f);
			yield return new WaitForSeconds(delay);
			_cube = Instantiate(prefab, transform, false);
			SetColor(_color);
			transform.localScale = voxelSize * Vector3.one;
			yield return new WaitForSeconds(Random.Range(3.0f, 4.0f));
			var boneColor = (byte)(bone * 0x255);
			SetColor(new VoxelColor32(boneColor, boneColor, boneColor));
		}

		private IEnumerator ActivationCoroutine(bool active) {
			var delay = Random.Range(0.2f, 1.0f);
			yield return new WaitForSeconds(delay);
			_cube.SetActive(active);
		}
		
		private IEnumerator ColorCoroutine() {
			var delay = Random.Range(0.2f, 1.0f);
			yield return new WaitForSeconds(delay);
			SetColor(_color);
		}
	}
}