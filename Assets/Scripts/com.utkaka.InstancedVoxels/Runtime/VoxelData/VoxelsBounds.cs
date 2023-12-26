using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
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
			if (point.x < _min.x) return false;
			if (point.y < _min.y) return false;
			if (point.z < _min.z) return false;
			if (point.x > _max.x) return false;
			if (point.y > _max.y) return false;
			return point.z <= _max.z;
		}
	}
}