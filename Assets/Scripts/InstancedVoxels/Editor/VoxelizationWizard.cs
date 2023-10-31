using System.IO;
using System.Linq;
using InstancedVoxels.VoxelData;
using InstancedVoxels.Voxelization;
using UnityEditor;
using UnityEngine;

namespace InstancedVoxels.Editor {
	public class VoxelizationWizard : ScriptableWizard {
		private static float _lastVoxelSize = 1.0f;
		private static string _lastSavePath;
		
		[SerializeField]
		private float _voxelSize;
		
		[MenuItem("Voxels/Voxelization Wizard")]
		private static void CreateWizard() {
			var wizard = DisplayWizard<VoxelizationWizard>("Voxelize GameObject", "Save", "Cancel");
			wizard._voxelSize = _lastVoxelSize;
		}

		private void OnWizardCreate() {
			_lastVoxelSize = _voxelSize;
			var selectedObject = Selection.activeObject as GameObject;
			if (selectedObject == null) return;
			var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
			var meshes = meshFilters.Select(f => f.sharedMesh).ToArray();
			var positions = meshFilters.Select(f => f.transform.position).ToArray();
			var textures = meshFilters
				.Select(f => f.GetComponent<MeshRenderer>().sharedMaterial.mainTexture as Texture2D).ToArray();
			
			var satVoxelizer = new Voxelizer(_voxelSize, meshes, positions, textures);
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			var voxels = satVoxelizer.Voxelize();
			watch.Stop();
			Debug.Log($"Voxelization Time: {watch.ElapsedMilliseconds} ms");
			
			var scriptableVoxelPath = EditorUtility.SaveFilePanelInProject("Save ScriptableVoxel", $"ScriptableVoxels.asset", "asset",
				"Please enter a file name to save the voxel", _lastSavePath);
			if (string.IsNullOrEmpty(scriptableVoxelPath)) return;
			_lastSavePath = Directory.GetParent(scriptableVoxelPath)?.FullName;
			var oldAsset = AssetDatabase.LoadAssetAtPath<Voxels>(scriptableVoxelPath);
			if (oldAsset == null) {
				AssetDatabase.CreateAsset(voxels, scriptableVoxelPath);	
			} else {
				oldAsset.CopyFrom(voxels);
				EditorUtility.SetDirty(oldAsset);
				AssetDatabase.SaveAssetIfDirty(oldAsset);
				DestroyImmediate(voxels);
			}
		}

		private void OnWizardOtherButton() {
			Close();
		}
	}
}