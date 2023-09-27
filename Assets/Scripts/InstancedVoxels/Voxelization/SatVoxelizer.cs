using System.Collections.Generic;
using System.Linq;
using InstancedVoxels.MeshData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.Voxelization {
	public class SatVoxelizer {
		private Bounds _bounds;
		private readonly Mesh[] _meshes;
		private readonly int3 _boxSize;
		private readonly float _voxelSize;
		private readonly Texture2D[] _textures;
		private readonly Vector3[] _positions;

		public int3 BoxSize => _boxSize;

		public Bounds Bounds => _bounds;

		public SatVoxelizer(float voxelSize, Mesh[] meshes, Vector3[] positions, Texture2D[] textures) {
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

		public NativeArray<Color> Voxelize() {
			var boxSizeYbyZ = _boxSize.y * _boxSize.z;
			using var meshData = Mesh.AcquireReadOnlyMeshData(_meshes);
			var boxSize = boxSizeYbyZ * _boxSize.z;
			using var satVoxels = new NativeArray<SatVoxel>(boxSize, Allocator.TempJob);
			var meshDataPositions = new NativeArray<float3>(meshData.Length, Allocator.TempJob);
			for (var i = 0; i < meshData.Length; i++) {
				meshDataPositions[i] = _positions[i];
			}
			
			var voxelColors = new NativeArray<Color>(boxSize, Allocator.TempJob);
			var satVoxelizerJob = new SatVoxelizerJob(_bounds.min, _boxSize, _voxelSize, meshData, meshDataPositions, satVoxels);
			var handle = satVoxelizerJob.Schedule();
			
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
			var read = new ReadVoxelColorJob(satVoxels, vertexUvReaders, textureDescriptors, textures, voxelColors);
			handle = read.Schedule(boxSize, batchCount, handle);
			handle.Complete();
			
			return voxelColors;
		}
	}
}