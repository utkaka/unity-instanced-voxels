using InstancedVoxels.Voxelization.Weights;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace InstancedVoxels.Voxelization.Compression {
	[BurstCompile]
	public struct CompressBonesJob : IJobFor {
		private int _boneIndex;
		[ReadOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		private NativeArray<int> _voxelBones;
		private NativeHashMap<int2, int> _compressedBones;

		public CompressBonesJob(NativeArray<WeightedVoxel> weightedVoxels, NativeArray<bool> outerVoxels, NativeArray<int> voxelBones, NativeHashMap<int2, int> compressedBones) : this() {
			_boneIndex = 0;
			_weightedVoxels = weightedVoxels;
			_outerVoxels = outerVoxels;
			_voxelBones = voxelBones;
			_compressedBones = compressedBones;
		}

		public void Execute(int index) {
			if (_outerVoxels[index]) return;
			var meshBone = new int2(_weightedVoxels[index].MeshIndex, _voxelBones[index]);
			if (!_compressedBones.ContainsKey(meshBone)) {
				_compressedBones.Add(meshBone, _boneIndex);
				_boneIndex++;
			}

			_voxelBones[index] = _compressedBones[meshBone];
		}
	}
}