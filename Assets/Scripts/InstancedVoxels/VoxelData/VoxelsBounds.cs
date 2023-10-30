using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace InstancedVoxels.VoxelData {
	[BurstCompile]
	public struct VoxelsBounds {
		private int3 _min;
		private int3 _max;

		public VoxelsBounds(int3 point) {
			_min = point;
			_max = point;
		}

		public VoxelsBounds(int3 min, int3 max) {
			_min = min;
			_max = max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(int3 point) {
			_min = math.min(_min, point);
			_max = math.max(_max, point);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int3 point) {
			return (point >= _min) is {x: true, y: true, z: true} && (point <= _max) is {x: true, y: true, z: true};
		}
	}
}