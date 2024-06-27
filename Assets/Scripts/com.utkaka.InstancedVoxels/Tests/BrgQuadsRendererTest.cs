using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering;
using com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests
{
    public class BrgQuadsRendererTest: VoxelRendererPerformanceTest<BrgQuadsRenderer> {
        private static IEnumerable TestCases() {
            yield return new RendererTestCase("Cube_008", CullingOptions.InnerSides);
            yield return new RendererTestCase("Cube_004", CullingOptions.InnerSides);
            yield return new RendererTestCase("Cube_002", CullingOptions.InnerSides);
            yield return new RendererTestCase("Cube_001", CullingOptions.InnerSides);
            yield return new RendererTestCase("Cube_1_div_255", CullingOptions.InnerSides);
            yield return new RendererTestCase("Spider_00072", CullingOptions.InnerSides);
        }

        [UnityTest, Performance]
        public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))]RendererTestCase testCase) {
            return Test(testCase);
        }
    }
}