using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Weights;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.Colors {
	[BurstCompile]
	public struct FillInnerBorderVoxelColorJob : IJob {
		private VoxelsBox _box;
		[ReadOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		[ReadOnly]
		private NativeArray<int> _voxelBones;
		private NativeArray<bool> _voxelColored;
		private NativeArray<byte3> _voxelColors;
		private NativeQueue<int> _borderVoxels;

		public FillInnerBorderVoxelColorJob(VoxelsBox box, NativeArray<WeightedVoxel> weightedVoxels, NativeArray<bool> outerVoxels, NativeArray<int> voxelBones, NativeArray<bool> voxelColored, NativeArray<byte3> voxelColors, NativeQueue<int> borderVoxels) {
			_box = box;
			_weightedVoxels = weightedVoxels;
			_outerVoxels = outerVoxels;
			_voxelBones = voxelBones;
			_voxelColored = voxelColored;
			_voxelColors = voxelColors;
			_borderVoxels = borderVoxels;
		}

		public void Execute() {
			for (var index = 0; index < _box.Count; index++) {
				if (_outerVoxels[index]) continue;
				if (_voxelColored[index]) continue;
				var bone = _voxelBones[index];
				var meshIndex = _weightedVoxels[index].MeshIndex;
				var back = _box.GetBack(index);
				var front = _box.GetFront(index);
				var top = _box.GetTop(index);
				var bottom = _box.GetBottom(index);
				var left = _box.GetLeft(index);
				var right = _box.GetRight(index);
				if (_voxelBones[back] == bone && _weightedVoxels[back].MeshIndex == meshIndex &&
				    _voxelBones[front] == bone && _weightedVoxels[front].MeshIndex == meshIndex &&
				    _voxelBones[top] == bone && _weightedVoxels[top].MeshIndex == meshIndex &&
				    _voxelBones[bottom] == bone && _weightedVoxels[bottom].MeshIndex == meshIndex &&
				    _voxelBones[left] == bone && _weightedVoxels[left].MeshIndex == meshIndex &&
				    _voxelBones[right] == bone && _weightedVoxels[right].MeshIndex == meshIndex) {
					continue;
				}
				_borderVoxels.Enqueue(index);
			}
			while (_borderVoxels.Count > 0) {
				var index = _borderVoxels.Dequeue();
				var back = _box.GetBack(index);
				var front = _box.GetFront(index);
				var top = _box.GetTop(index);
				var bottom = _box.GetBottom(index);
				var left = _box.GetLeft(index);
				var right = _box.GetRight(index);
				if (_voxelColored[back]) {
					_voxelColors[index] = _voxelColors[back];
					_voxelColored[index] = true;
					continue;
				}
				if (_voxelColored[front]) {
					_voxelColors[index] = _voxelColors[front];
					_voxelColored[index] = true;
					continue;
				}
				if (_voxelColored[top]) {
					_voxelColors[index] = _voxelColors[top];
					_voxelColored[index] = true;
					continue;
				}
				if (_voxelColored[bottom]) {
					_voxelColors[index] = _voxelColors[bottom];
					_voxelColored[index] = true;
					continue;
				}
				if (_voxelColored[left]) {
					_voxelColors[index] = _voxelColors[left];
					_voxelColored[index] = true;
					continue;
				}
				if (_voxelColored[right]) {
					_voxelColors[index] = _voxelColors[right];
					_voxelColored[index] = true;
					continue;
				}
				_borderVoxels.Enqueue(index);
			}
		}
	}
}