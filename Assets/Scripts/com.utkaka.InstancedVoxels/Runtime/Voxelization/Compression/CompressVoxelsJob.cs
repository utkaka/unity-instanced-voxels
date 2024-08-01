using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.Compression {
	[BurstCompile]
	public struct CompressVoxelsJob : IJobFor {
		private VoxelsBox _box;
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		[ReadOnly]
		private NativeArray<int> _voxelBones;
		[ReadOnly]
		private NativeArray<byte3> _voxelColors;
		private NativeArray<bool> _processedVoxels;
		[WriteOnly]
		private NativeList<VoxelPlain> _plainVoxels;
		[WriteOnly]
		private NativeList<VoxelCompressed> _compressedVoxels;

		public CompressVoxelsJob(VoxelsBox box, NativeArray<bool> outerVoxels, NativeArray<int> voxelBones,
			NativeArray<byte3> voxelColors, NativeArray<bool> processedVoxels, NativeList<VoxelPlain> plainVoxels,
			NativeList<VoxelCompressed> compressedVoxels) : this() {
			_box = box;
			_outerVoxels = outerVoxels;
			_voxelBones = voxelBones;
			_voxelColors = voxelColors;
			_processedVoxels = processedVoxels;
			_plainVoxels = plainVoxels;
			_compressedVoxels = compressedVoxels;
		}

		public void Execute(int index) {
			if (_outerVoxels[index]) return;
			if (_processedVoxels[index]) return;
			var position = _box.GetVoxelPosition(index);
			var bone = _voxelBones[index];
			var color = _voxelColors[index];

			var complexSize = new int3(1, 1, 1);
			
			var rightIndex = index;
			for (var i = position.x + 1; i < _box.Size.x; i++) {
				rightIndex = _box.GetRight(rightIndex);
				if (_outerVoxels[rightIndex] || _processedVoxels[rightIndex] || _voxelBones[rightIndex] != bone ||
				    _voxelColors[rightIndex] != color) break;
				complexSize.x++;
			}
			
			var topIndex = index;
			for (var i = position.y + 1; i < _box.Size.y; i++) {
				topIndex = _box.GetTop(topIndex);
				rightIndex = topIndex;
				var sameVoxels = true;
				for (var j = 0; j < complexSize.x; j++) {
					if (_outerVoxels[rightIndex] || _processedVoxels[rightIndex] || _voxelBones[rightIndex] != bone ||
					    _voxelColors[rightIndex] != color) {
						sameVoxels = false;
						break;
					}
					rightIndex = _box.GetRight(rightIndex);
				}
				if (!sameVoxels) break;
				complexSize.y++;
			}
			
			var frontIndex = index;
			for (var i = position.z + 1; i < _box.Size.z; i++) {
				frontIndex = _box.GetFront(frontIndex);
				topIndex = frontIndex;
				var sameVoxels = true;
				for (var j = 0; j < complexSize.y; j++) {
					rightIndex = topIndex;
					for (var k = 0; k < complexSize.x; k++) {
						if (_outerVoxels[rightIndex] || _processedVoxels[rightIndex] || _voxelBones[rightIndex] != bone ||
						    _voxelColors[rightIndex] != color) {
							sameVoxels = false;
							break;
						}
						rightIndex = _box.GetRight(rightIndex);
					}
					if (!sameVoxels) break;
					topIndex = _box.GetTop(topIndex);
				}
				if (!sameVoxels) break;
				complexSize.z++;
			}
			
			frontIndex = index;
			for (var i = 0; i < complexSize.z; i++) {
				topIndex = frontIndex;
				for (var j = 0; j < complexSize.y; j++) {
					rightIndex = topIndex;
					for (var k = 0; k < complexSize.x; k++) {
						_processedVoxels[rightIndex] = true;
						rightIndex = _box.GetRight(rightIndex);
					}
					topIndex = _box.GetTop(topIndex);
				}
				frontIndex = _box.GetFront(frontIndex);
			}

			if (complexSize is { x: 1, y: 1, z: 1 }) {
				_plainVoxels.AddNoResize(new VoxelPlain(position, bone, color));
				return;
			}
			
			_compressedVoxels.AddNoResize(new VoxelCompressed(position, complexSize, bone, color));
		}
	}
}