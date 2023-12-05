using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.Colors {
	public struct FillInnerVoxelColorJob : IJobParallelFor {
		private readonly int _innerColorsCount;
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		[ReadOnly]
		private NativeArray<bool> _voxelColored;
		[ReadOnly]
		private NativeArray<byte3> _innerColors;
		[WriteOnly]
		private NativeArray<byte3> _voxelColors;
		private Random _random;

		public FillInnerVoxelColorJob(int innerColorsCount, NativeArray<bool> outerVoxels, NativeArray<bool> voxelColored,
			NativeArray<byte3> innerColors, NativeArray<byte3> voxelColors, Random random) {
			_outerVoxels = outerVoxels;
			_innerColorsCount = innerColorsCount;
			_voxelColored = voxelColored;
			_innerColors = innerColors;
			_voxelColors = voxelColors;
			_random = random;
		}

		public void Execute(int index) {
			if (_voxelColored[index]) return;
			if (_outerVoxels[index]) return;
			_voxelColors[index] = _innerColors[_random.NextInt(0, _innerColorsCount)];
		}
	}
}