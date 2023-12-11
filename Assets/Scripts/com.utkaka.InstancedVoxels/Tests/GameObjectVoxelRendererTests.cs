using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests {
	public class GameObjectVoxelRendererTests : VoxelRendererPerformanceTest<GameObjectVoxelRenderer> {
		private static IEnumerable TestCases() {
			yield return "008";
			yield return "004";
			yield return "002";
		}

		[UnityTest, Performance]
		public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))]string voxelSize) {
			return Test(voxelSize);
		}
	}
}