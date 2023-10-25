using System.Collections.Generic;
using InstancedVoxels.MeshData;
using InstancedVoxels.VoxelData;
using InstancedVoxels.Voxelization.Bones;
using InstancedVoxels.Voxelization.Colors;
using InstancedVoxels.Voxelization.Compression;
using InstancedVoxels.Voxelization.Sat;
using InstancedVoxels.Voxelization.Weights;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.Voxelization {
	public class Voxelizer {
		private Bounds _bounds;
		private readonly Mesh[] _meshes;
		private readonly int3 _boxSize;
		private readonly float _voxelSize;
		private readonly Texture2D[] _textures;
		private readonly Vector3[] _positions;

		public int3 BoxSize => _boxSize;

		public Bounds Bounds => _bounds;

		public Voxelizer(float voxelSize, Mesh[] meshes, Vector3[] positions, Texture2D[] textures) {
			_voxelSize = voxelSize;
			_bounds = new Bounds();
			_positions = positions;
			for (var i = 0; i < meshes.Length; i++) {
				var mesh = meshes[i];
				var bounds = mesh.bounds;
				bounds.center += positions[i];
				_bounds.Encapsulate(bounds);
			}

			_boxSize = new int3((int)(_bounds.size.x / _voxelSize) + 1, (int)(_bounds.size.y / _voxelSize) + 1,
				(int)(_bounds.size.z / _voxelSize) + 1);
			_meshes = meshes;
			_textures = textures;
		}

		public Voxels Voxelize() {
			var boxSize = _boxSize.y * _boxSize.z * _boxSize.z;
			
			var meshData = Mesh.AcquireReadOnlyMeshData(_meshes);
			var satVoxels = new NativeArray<SatVoxel>(boxSize, Allocator.TempJob);
			var createVoxelsHandle = CreateSatVoxels(meshData, satVoxels);
			
			var compressedVoxels = new NativeList<CompressedVoxel>(boxSize, Allocator.TempJob);
			var voxelIndices = new NativeList<int>(boxSize, Allocator.TempJob);
			var compressVoxelsHandle = CompressSatVoxels(satVoxels, compressedVoxels, voxelIndices, createVoxelsHandle);
			compressVoxelsHandle.Complete();
			
			var weightedVoxels = new NativeArray<WeightedVoxel>(compressedVoxels.Length, Allocator.TempJob);
			var calculateVoxelWeightsHandle = CalculateVoxelWeights(compressedVoxels.AsArray(), weightedVoxels, default);
			
			var voxelColors = new NativeArray<float3>(compressedVoxels.Length, Allocator.TempJob);
			var readVoxelColorsHandle = ReadVoxelColors(voxelColors, meshData, weightedVoxels, calculateVoxelWeightsHandle);
			
			var voxelBones = new NativeArray<int>(compressedVoxels.Length, Allocator.TempJob);
			var boneWeights = new NativeHashMap<int, float>(255 * 3, Allocator.TempJob);
			ReadVoxelBones(voxelBones, meshData, weightedVoxels, boneWeights, readVoxelColorsHandle).Complete();

			var voxels = Voxels.Create(_boxSize, _bounds.min, _voxelSize, voxelIndices, voxelColors, voxelBones);
				
			meshData.Dispose();
			satVoxels.Dispose();
			compressedVoxels.Dispose();
			weightedVoxels.Dispose();
			voxelIndices.Dispose();
			voxelColors.Dispose();
			voxelBones.Dispose();
			boneWeights.Dispose();
			
			return voxels;
		}

		private JobHandle CreateSatVoxels(Mesh.MeshDataArray meshData, NativeArray<SatVoxel> satVoxels) {
			var meshDataPositions = new NativeArray<float3>(meshData.Length, Allocator.TempJob);
			for (var i = 0; i < meshData.Length; i++) {
				meshDataPositions[i] = _positions[i];
			}
			
			var satVoxelizerJob = new SatVoxelizerJob(_bounds.min, _boxSize, _voxelSize, meshData, meshDataPositions, satVoxels);
			return satVoxelizerJob.Schedule();
		}
		
		private JobHandle CompressSatVoxels(NativeArray<SatVoxel> satVoxels, NativeList<CompressedVoxel> compressedVoxels, NativeList<int> voxelIndices, JobHandle jobDependency) {
			var compressJob = new CompressVoxelsJob(satVoxels, compressedVoxels, voxelIndices);
			return compressJob.Schedule(satVoxels.Length, jobDependency);
		}

		private JobHandle CalculateVoxelWeights(NativeArray<CompressedVoxel> compressedVoxels, NativeArray<WeightedVoxel> weightedVoxels, JobHandle jobDependency) {
			var weightsJob = new CalculateVoxelWeightsJob(compressedVoxels, weightedVoxels);
			var batchCount = compressedVoxels.Length / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			return weightsJob.Schedule(compressedVoxels.Length, batchCount, jobDependency);
		}

		private JobHandle ReadVoxelColors(NativeArray<float3> voxelColors, Mesh.MeshDataArray meshData, NativeArray<WeightedVoxel> weightedVoxels, JobHandle jobDependency) {
			var boxSize = voxelColors.Length;
			var whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			whiteTexture.SetPixel(0, 0, Color.white);
			whiteTexture.Apply();
			
			var texturesDictionary = new Dictionary<Texture2D, TextureDescriptor>();
			var texturesCopied = new Dictionary<Texture2D, bool>();
			
			texturesDictionary.Add(whiteTexture, new TextureDescriptor(0, 1, 1));
			texturesCopied.Add(whiteTexture, false);
			
			var texturesSize = 1;
			for (var i = 0; i < _textures.Length; i++) {
				var texture = _textures[i];
				if (texture == null) texture = whiteTexture;
				if (texturesDictionary.ContainsKey(texture)) continue;
				texturesDictionary.Add(texture, new TextureDescriptor(texturesSize, texture.width, texture.height));
				texturesCopied.Add(texture, false);
				texturesSize += texture.width * texture.height;
			}

			var textureDescriptors = new NativeArray<TextureDescriptor>(meshData.Length, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory);
			var vertexUvReaders = new NativeArray<VertexUVReader>(meshData.Length, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory);
			var textures =
				new NativeArray<Color>(texturesSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			
			for (var i = 0; i < _textures.Length; i++) {
				var texture = _textures[i];
				if (texture == null) texture = whiteTexture;
				var descriptor = texturesDictionary[texture];
				textureDescriptors[i] = descriptor;
				vertexUvReaders[i] = new VertexUVReader(meshData[i]);
				if (texturesCopied[texture]) continue;
				texturesCopied[texture] = true;
				var texturePixels = texture.GetPixels();
				NativeArray<Color>.Copy(texturePixels, 0, textures, descriptor.StartIndex, texturePixels.Length);
			}
			
			
			var batchCount = boxSize / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var read = new ReadVoxelColorJob(weightedVoxels, vertexUvReaders, textureDescriptors, textures, voxelColors);
			return read.Schedule(boxSize, batchCount, jobDependency);
		}

		private JobHandle ReadVoxelBones(NativeArray<int> voxelBones, Mesh.MeshDataArray meshData,
			NativeArray<WeightedVoxel> weightedVoxels, NativeHashMap<int, float> boneWeights, JobHandle jobDependency) {
			var boxSize = voxelBones.Length;

			var boneReaders = new NativeArray<VertexBonesReader>(meshData.Length, Allocator.TempJob);
			for (var i = 0; i < meshData.Length; i++) {
				boneReaders[i] = new VertexBonesReader(meshData[i]);
			}
			
			var read = new ReadVoxelBoneJob(weightedVoxels, boneReaders, boneWeights, voxelBones);
			return read.Schedule(boxSize, jobDependency);
		}
	}
}