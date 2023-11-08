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
			var selectedObject = Selection.objects.OfType<GameObject>().ToArray();
			var meshObjects = selectedObject.SelectMany(so => so.GetComponentsInChildren<MeshFilter>())
				.Select(mf => mf.gameObject)
				.Concat(selectedObject.SelectMany(so => so.GetComponentsInChildren<SkinnedMeshRenderer>())
					.Select(sm => sm.gameObject));
			
			var satVoxelizer = new Voxelizer(_voxelSize, meshObjects.ToArray());
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