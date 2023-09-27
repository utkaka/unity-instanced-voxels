using System.Linq;
using InstancedVoxels.Voxelization;
using UnityEditor;
using UnityEngine;

namespace InstancedVoxels.Editor {
	public class VoxelizationWizard {
		[MenuItem("Voxels/Voxelization Wizard")]
		public static void TestVoxelization() {
			var selectedObject = Selection.activeObject as GameObject;
			if (selectedObject == null) return;
			var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
			var meshes = meshFilters.Select(f => f.sharedMesh).ToArray();
			var positions = meshFilters.Select(f => f.transform.position).ToArray();
			var textures = meshFilters
				.Select(f => f.GetComponent<MeshRenderer>().sharedMaterial.mainTexture as Texture2D).ToArray();
			
			var voxelSize = 0.01f;
			var satVoxelizer = new SatVoxelizer(voxelSize, meshes, positions, textures);
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			var box = satVoxelizer.Voxelize();
			watch.Stop();
			Debug.Log($"Execution Time: {watch.ElapsedMilliseconds} ms");

			var scriptableVoxels =
				ScriptableVoxels.Create(voxelSize, satVoxelizer.Bounds.min, satVoxelizer.BoxSize, box.ToArray());
			box.Dispose();
			var scriptableVoxelPath = EditorUtility.SaveFilePanelInProject("Save ScriptableVoxel", $"ScriptableVoxels.asset", "asset",
				"Please enter a file name to save the voxel");
			if (string.IsNullOrEmpty(scriptableVoxelPath)) return;
			AssetDatabase.CreateAsset(scriptableVoxels, scriptableVoxelPath);
		}
	}
}