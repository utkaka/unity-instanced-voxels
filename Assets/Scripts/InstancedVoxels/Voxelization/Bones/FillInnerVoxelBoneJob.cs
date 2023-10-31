using InstancedVoxels.VoxelData;
using InstancedVoxels.Voxelization.Weights;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.Voxelization.Bones {
	[BurstCompile]
	public struct FillInnerVoxelBoneJob : IJob {
		private VoxelsBox _box;
		private NativeArray<WeightedVoxel> _weightedVoxels;
		private NativeArray<int> _voxelBones;
		[ReadOnly]
		private NativeArray<bool> _outerVoxels;
		private NativeHashMap<int2, VoxelsBounds> _boundsByBone;
		private NativeQueue<int> _multiBoneVoxels; 
		private NativeHashMap<int2, int> _neighbourBones;

		public FillInnerVoxelBoneJob(VoxelsBox box, NativeArray<WeightedVoxel> weightedVoxels,
			NativeArray<int> voxelBones, NativeArray<bool> outerVoxels, NativeHashMap<int2, VoxelsBounds> boundsByBone,
			NativeQueue<int> multiBoneVoxels, NativeHashMap<int2, int> neighbourBones) {
			_box = box;
			_weightedVoxels = weightedVoxels;
			_voxelBones = voxelBones;
			_outerVoxels = outerVoxels;
			_boundsByBone = boundsByBone;
			_multiBoneVoxels = multiBoneVoxels;
			_neighbourBones = neighbourBones;
		}

		public void Execute() {
			//Get bounds for bones
			for (var x = 0; x < _box.Size.x; x++) {
				for (var y = 0; y < _box.Size.y; y++) {
					for (var z = 0; z < _box.Size.z; z++) {
						var position = new int3(x, y, z);
						var index = _box.GetVoxelIndex(position);
						if (_outerVoxels[index]) continue;
						var meshIndex = _weightedVoxels[index].MeshIndex;
						if (meshIndex == 0) continue;
						var bone = new int2(meshIndex, _voxelBones[index]);
						if (!_boundsByBone.ContainsKey(bone)) {
							_boundsByBone.Add(bone, new VoxelsBounds(position));	
						} else {
							var bounds = _boundsByBone[bone];
							bounds.Encapsulate(position);
							_boundsByBone[bone] = bounds;
						}
					}
				}
			}
			//Fill single bone voxels and add all voxels with multiple bones to the queue
			for (var x = 0; x < _box.Size.x; x++) {
				for (var y = 0; y < _box.Size.y; y++) {
					for (var z = 0; z < _box.Size.z; z++) {
						var position = new int3(x, y, z);
						var index = _box.GetVoxelIndex(position);
						if (_outerVoxels[index]) continue;
						if (_weightedVoxels[index].MeshIndex > 0) continue;
						var hasConflict = false;
						var bone = new int2(0, -1);
						foreach (var boneBounds in _boundsByBone) {
							if (!boneBounds.Value.Contains(position)) continue;
							if (bone.x > 0) {
								hasConflict = true;
								break;
							}
							bone = boneBounds.Key;
						}
						if (!hasConflict) {
							_weightedVoxels[index] = new WeightedVoxel(_weightedVoxels[index], bone.x);
							_voxelBones[index] = bone.y;
						} else {
							_multiBoneVoxels.Enqueue(index);
						}
					}
				}
			}
			
			//Fill multiple bones voxels by the most frequent bone of their neighbours
			while (_multiBoneVoxels.Count > 0) {
				_neighbourBones.Clear();
				var voxel = _multiBoneVoxels.Dequeue();
				FillNeighbourBone(_box.GetBack(voxel));
				FillNeighbourBone(_box.GetBottom(voxel));
				FillNeighbourBone(_box.GetFront(voxel));
				FillNeighbourBone(_box.GetLeft(voxel));
				FillNeighbourBone(_box.GetRight(voxel));
				FillNeighbourBone(_box.GetTop(voxel));
				var maxBones = 0;
				var maxBone = new int2(0, 0);
				var totalBones = 0;
				foreach (var boneCount in _neighbourBones) {
					totalBones += boneCount.Value;
					if (maxBones >= boneCount.Value) continue;
					maxBones = boneCount.Value;
					maxBone = boneCount.Key;	
				}

				if (maxBones >= 2 || totalBones >= 6) {
					_weightedVoxels[voxel] = new WeightedVoxel(_weightedVoxels[voxel], maxBone.x);
					_voxelBones[voxel] = maxBone.y;
				} else {
					_multiBoneVoxels.Enqueue(voxel);
				}
			}
		}

		private void FillNeighbourBone(int index) {
			var meshIndex = _weightedVoxels[index].MeshIndex;
			if (meshIndex == 0) return;
			var bone = new int2(meshIndex, _voxelBones[index]);
			if (!_neighbourBones.ContainsKey(bone)) _neighbourBones.Add(bone, 1);
			else _neighbourBones[bone]++;
		}
	}
}