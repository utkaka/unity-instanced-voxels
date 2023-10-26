using InstancedVoxels.MeshData;
using InstancedVoxels.Voxelization.Weights;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace InstancedVoxels.Voxelization.Bones {
	[BurstCompile]
	public struct ReadVoxelBoneJob : IJobFor {
		[ReadOnly]
		private NativeArray<WeightedVoxel> _weightedVoxels;
		[ReadOnly, DeallocateOnJobCompletion]
		private NativeArray<VertexBonesReader> _bonesReaders;
		
		private NativeHashMap<int, float> _boneWeights;
		[WriteOnly]
		private NativeArray<int> _voxelBones;

		public ReadVoxelBoneJob(NativeArray<WeightedVoxel> weightedVoxels, NativeArray<VertexBonesReader> bonesReaders, NativeHashMap<int, float> boneWeights, NativeArray<int> voxelBones) {
			_weightedVoxels = weightedVoxels;
			_bonesReaders = bonesReaders;
			_boneWeights = boneWeights;
			_voxelBones = voxelBones;
		}

		public void Execute(int index) {
			_boneWeights.Clear();
			var weightedVoxel = _weightedVoxels[index];
			var meshIndex = weightedVoxel.MeshIndex - 1;
			if (meshIndex < 0) return;
			var bonesReader = _bonesReaders[meshIndex];

			FillBoneWeights(bonesReader, weightedVoxel.VertexIndex0, weightedVoxel.VertexWeight0);
			FillBoneWeights(bonesReader, weightedVoxel.VertexIndex1, weightedVoxel.VertexWeight1);
			FillBoneWeights(bonesReader, weightedVoxel.VertexIndex2, weightedVoxel.VertexWeight2);
			
			var maxBoneWeight = float.MinValue;
			var maxBoneIndex = -1;
			foreach (var bone in _boneWeights) {
				if (bone.Value <= maxBoneWeight) continue;
				maxBoneWeight = bone.Value;
				maxBoneIndex = bone.Key;
			}

			_voxelBones[index] = maxBoneIndex;
		}

		private void FillBoneWeights(VertexBonesReader bonesReader, int vertexIndex, float vertexWeight) {
			unsafe {
				var vertexBoneIndexPointer = bonesReader.GetVertexBoneIndexPointer(vertexIndex);
				var vertexBoneWeightPointer = bonesReader.GetVertexBoneWeightPointer(vertexIndex);
				for (var i = 0; i < bonesReader.BonesCount; i++) {
					var boneIndex = bonesReader.GetVertexBoneIndex(vertexBoneIndexPointer);
					var boneWeight = bonesReader.GetVertexBoneWeight(vertexBoneWeightPointer) * vertexWeight;
					if (!_boneWeights.ContainsKey(boneIndex)) _boneWeights.Add(boneIndex, boneWeight);
					else _boneWeights[boneIndex] += boneWeight;
					vertexBoneIndexPointer += bonesReader.IndexSize;
					vertexBoneWeightPointer += bonesReader.WeightSize;
				}
			}
		}
	}
}