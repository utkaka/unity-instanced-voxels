using System;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	public enum CullingOptions {
		None = 0,
		InnerVoxels = 1,
		InnerSides = 2,
		InnerSidesAndBackface = 3,
		InnerSidesAndBackfaceUpdate = 4
	}
}