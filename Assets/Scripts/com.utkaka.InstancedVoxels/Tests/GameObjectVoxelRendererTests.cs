using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering;
using com.utkaka.InstancedVoxels.Runtime.Rendering.SeparateGameObjects;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests {
	public class GameObjectVoxelRendererTests : VoxelRendererPerformanceTest<GameObjectVoxelRenderer> {
		private static IEnumerable TestCases() {
			yield return new RendererTestCase("008", CullingOptions.None);
			yield return new RendererTestCase("004", CullingOptions.None);
			yield return new RendererTestCase("002", CullingOptions.None);
		}

		[UnityTest, Performance]
		public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))]RendererTestCase voxelSize) {
			return Test(voxelSize);
		}
	}
}