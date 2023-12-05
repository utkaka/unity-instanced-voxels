using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests {
	public class GameObjectVoxelRendererTests {
		[UnityTest, Performance]
		public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))] string voxelSize) {
			var cameraTransform = new GameObject("Main Camera", typeof(Camera)).transform;
			cameraTransform.gameObject.tag = "MainCamera";
			cameraTransform.position = new Vector3(0.38f, 1.17f, -1.44f);
			cameraTransform.rotation = Quaternion.Euler(32.06f, -12.81f, 0.0f);

			var renderer = new GameObject("Renderer", typeof(GameObjectVoxelRenderer))
				.GetComponent<GameObjectVoxelRenderer>();
			
			var voxels = Resources.Load<Voxels>($"Voxel_{voxelSize}");
			var cube = Resources.Load<GameObject>($"Voxel");
			renderer.Init(voxels, cube);
			yield return Measure.Frames()
				.WarmupCount(60)
				.MeasurementCount(60)
				.Run();
			Object.Destroy(renderer.gameObject);
			Object.Destroy(cameraTransform.gameObject);
		}
		
		private static IEnumerable TestCases() {
			yield return "008";
			yield return "004";
			yield return "002";
		}
	}
}