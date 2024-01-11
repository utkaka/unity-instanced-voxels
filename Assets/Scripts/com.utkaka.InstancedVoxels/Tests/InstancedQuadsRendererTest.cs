using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering;
using com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests {
	public class InstancedQuadsRendererTest : VoxelRendererPerformanceTest<InstancedQuadsRenderer> {
		private static IEnumerable TestCases() {
			yield return new RendererTestCase("Cube_008", CullingOptions.InnerSides);
			yield return new RendererTestCase("Cube_004", CullingOptions.InnerSides);
			yield return new RendererTestCase("Cube_002", CullingOptions.InnerSides);
			yield return new RendererTestCase("Cube_001", CullingOptions.InnerSides);
			yield return new RendererTestCase("Cube_1_div_255", CullingOptions.InnerSides);
			yield return new RendererTestCase("Spider_00072", CullingOptions.InnerSides);
			yield return new RendererTestCase("Cube_008", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("Cube_004", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("Cube_002", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("Cube_001", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("Cube_1_div_255", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("Spider_00072", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("Cube_008", CullingOptions.InnerSidesAndBackfaceUpdate);
			yield return new RendererTestCase("Cube_004", CullingOptions.InnerSidesAndBackfaceUpdate);
			yield return new RendererTestCase("Cube_002", CullingOptions.InnerSidesAndBackfaceUpdate);
			yield return new RendererTestCase("Cube_001", CullingOptions.InnerSidesAndBackfaceUpdate);
			yield return new RendererTestCase("Cube_1_div_255", CullingOptions.InnerSidesAndBackfaceUpdate);
			yield return new RendererTestCase("Spider_00072", CullingOptions.InnerSidesAndBackfaceUpdate);
		}

		[UnityTest, Performance]
		public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))]RendererTestCase testCase) {
			return Test(testCase);
		}
	}
}