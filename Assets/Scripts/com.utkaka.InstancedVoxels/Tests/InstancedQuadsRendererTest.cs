using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering;
using com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedQuad;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests {
	public class InstancedQuadsRendererTest : VoxelRendererPerformanceTest<InstancedQuadsRenderer> {
		private static IEnumerable TestCases() {
			yield return new RendererTestCase("008", CullingOptions.None);
			yield return new RendererTestCase("004", CullingOptions.None);
			yield return new RendererTestCase("002", CullingOptions.None);
			yield return new RendererTestCase("001", CullingOptions.None);
			yield return new RendererTestCase("1_div_255", CullingOptions.None);
			yield return new RendererTestCase("008", CullingOptions.InnerVoxels);
			yield return new RendererTestCase("004", CullingOptions.InnerVoxels);
			yield return new RendererTestCase("002", CullingOptions.InnerVoxels);
			yield return new RendererTestCase("001", CullingOptions.InnerVoxels);
			yield return new RendererTestCase("1_div_255", CullingOptions.InnerVoxels);
			yield return new RendererTestCase("008", CullingOptions.InnerSides);
			yield return new RendererTestCase("004", CullingOptions.InnerSides);
			yield return new RendererTestCase("002", CullingOptions.InnerSides);
			yield return new RendererTestCase("001", CullingOptions.InnerSides);
			yield return new RendererTestCase("1_div_255", CullingOptions.InnerSides);
			yield return new RendererTestCase("008", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("004", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("002", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("001", CullingOptions.InnerSidesAndBackface);
			yield return new RendererTestCase("1_div_255", CullingOptions.InnerSidesAndBackface);
		}

		[UnityTest, Performance]
		public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))]RendererTestCase testCase) {
			return Test(testCase);
		}
	}
}