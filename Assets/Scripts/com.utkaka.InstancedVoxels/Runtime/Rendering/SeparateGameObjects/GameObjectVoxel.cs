using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects {
	public class GameObjectVoxel : MonoBehaviour{
		public void SetupVoxel(GameObject prefab, byte3 color, float bone, float voxelSize) {
			var meshRenderer = Instantiate(prefab, transform, false).GetComponent<MeshRenderer>();
			meshRenderer.material.color = new Color(color.x / 255.0f, color.y / 255.0f, color.z / 255.0f);
			transform.localScale = voxelSize * Vector3.one;
		}
	}
}