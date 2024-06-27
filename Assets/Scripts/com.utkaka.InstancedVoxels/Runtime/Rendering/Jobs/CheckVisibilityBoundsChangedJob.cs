using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct CheckVisibilityBoundsChangedJob : IJobFor {
		private bool _boundsChanged;
		[ReadOnly]
		private NativeSlice<VoxelsBounds> _previousBounds;
		[ReadOnly]
		private NativeSlice<VoxelsBounds> _currentBounds;
		[WriteOnly]
		private NativeArray<bool> _boundsChangedOutput;

		public CheckVisibilityBoundsChangedJob(NativeSlice<VoxelsBounds> previousBounds,
			NativeSlice<VoxelsBounds> currentBounds, NativeArray<bool> boundsChangedOutput) {
			_boundsChanged = false;
			_previousBounds = previousBounds;
			_currentBounds = currentBounds;
			_boundsChangedOutput = boundsChangedOutput;
		}

		public void Execute(int index) {
			if (_boundsChanged) return;
			var previousBound = _previousBounds[index];
			var currentBound = _currentBounds[index];
			if (previousBound.Min.Equals(currentBound.Min)  && previousBound.Max.Equals(currentBound.Max)) return;
			_boundsChanged = true;
			_boundsChangedOutput[0] = true;
		}
	}
}