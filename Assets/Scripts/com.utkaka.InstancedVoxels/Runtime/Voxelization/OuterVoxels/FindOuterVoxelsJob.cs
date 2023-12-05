using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Weights;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.OuterVoxels {
	[BurstCompile]
	public struct FindOuterVoxelsJob : IJob {
		private VoxelsBox _box;
		[ReadOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;
		private NativeQueue<int3> _queue;
		private NativeArray<bool> _outerVoxels;

		public FindOuterVoxelsJob(VoxelsBox box, NativeArray<WeightedVoxel> weightedVoxels, NativeQueue<int3> queue, NativeArray<bool> outerVoxels) {
			_box = box;
			_weightedVoxels = weightedVoxels;
			_queue = queue;
			_outerVoxels = outerVoxels;
		}

		public void Execute() {
			_queue.Enqueue(int3.zero);
			while (_queue.Count > 0) {
				var cell = _queue.Dequeue();
				var index = _box.GetVoxelIndex(cell);
				if (_weightedVoxels[index].MeshIndex > 0 || _outerVoxels[index]) continue;
				_outerVoxels[index] = true;
				OuterVoxelsEnqueue(new int3(cell.x + 1, cell.y, cell.z));
				OuterVoxelsEnqueue(new int3(cell.x - 1, cell.y, cell.z));
				OuterVoxelsEnqueue(new int3(cell.x, cell.y + 1, cell.z));
				OuterVoxelsEnqueue(new int3(cell.x, cell.y - 1, cell.z));
				OuterVoxelsEnqueue(new int3(cell.x, cell.y, cell.z + 1));
				OuterVoxelsEnqueue(new int3(cell.x, cell.y, cell.z - 1));
			}
		}

		private void OuterVoxelsEnqueue(int3 position) {
			if (position.x < 0 || position.y < 0 || position.z < 0) return;
			if (position.x >= _box.Size.x || position.y >= _box.Size.y || position.z >= _box.Size.z) return;
			_queue.Enqueue(position);
		}
	}
}