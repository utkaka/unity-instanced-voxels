using System;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	[Flags]
	public enum CullingOptions {
		None = 0,
		InnerVoxels = 1
	}
}