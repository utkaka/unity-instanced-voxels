using System.Collections;
using com.utkaka.InstancedVoxels.Runtime.Rendering.InstancedCube;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace com.utkaka.InstancedVoxels.Tests {
	public class InstancedCubesRendererTests : VoxelRendererPerformanceTest<InstancedCubesRenderer> {
		private static IEnumerable TestCases() {
			yield return "008";
			yield return "004";
			yield return "002";
			yield return "001";
			yield return "1_div_255";
		}

		[UnityTest, Performance]
		public IEnumerator PerformanceTest([ValueSource(nameof(TestCases))]string voxelSize) {
			return Test(voxelSize);
		}
	}
}