using System.Collections.Generic;
using com.utkaka.InstancedVoxels.Runtime.MeshData;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Bones;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Colors;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Compression;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.OuterVoxels;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Sat;
using com.utkaka.InstancedVoxels.Runtime.Voxelization.Weights;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization {
	public class Voxelizer {
		private Bounds _bounds;
		private readonly Mesh[] _meshes;
		private readonly VoxelsBox _box;
		private readonly float _voxelSize;
		private readonly Texture2D[] _textures;
		private readonly Vector3[] _positions;
		private readonly Animation[] _animations;
		private readonly Transform[][] _boneTransforms;

		public VoxelsBox Box => _box;

		public Bounds Bounds => _bounds;

		public Voxelizer(float voxelSize, GameObject[] gameObjects) {
			var meshCount = gameObjects.Length;
			_positions = new Vector3[meshCount];
			_meshes = new Mesh[meshCount];
			_textures = new Texture2D[meshCount];
			_animations = new Animation[meshCount];
			_boneTransforms = new Transform[meshCount][];
			PrepareData(gameObjects);
			_voxelSize = voxelSize;
			_bounds = new Bounds(_meshes[0].bounds.center + _positions[0], Vector3.zero);
			for (var i = 0; i < meshCount; i++) {
				var mesh = _meshes[i];
				var bounds = mesh.bounds;
				bounds.center += _positions[i];
				_bounds.Encapsulate(bounds);
			}
			_box = new VoxelsBox(new int3((int)(_bounds.size.x / _voxelSize) + 3, (int)(_bounds.size.y / _voxelSize) + 3,
				(int)(_bounds.size.z / _voxelSize) + 3));
		}

		private void PrepareData(GameObject[] gameObjects) {
			for (var i = 0; i < gameObjects.Length; i++) {
				var gameObject = gameObjects[i];
				_positions[i] = gameObject.transform.position;
				var skinnedMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (skinnedMesh != null) {
					_textures[i] = skinnedMesh.sharedMaterial.mainTexture as Texture2D;
					_boneTransforms[i] = skinnedMesh.bones;
					var animation = gameObject.transform.parent != null
						? gameObject.transform.parent.GetComponent<Animation>()
						: null;
					if (animation != null) {
						_animations[i] = animation;
						var bakedMesh = new Mesh();
						var clip = animation.clip;
						animation.Play(clip.name);
						var animationState = animation[clip.name];
						animationState.time = 0.0f;
						animation.Sample();
						skinnedMesh.BakeMesh(bakedMesh);
						bakedMesh.boneWeights = skinnedMesh.sharedMesh.boneWeights;
						_meshes[i] = bakedMesh;
					} else {
						_meshes[i] = skinnedMesh.sharedMesh;		
					}
					continue;
				}
				var meshFilter = gameObject.GetComponent<MeshFilter>();
				if (meshFilter == null) continue;
				_boneTransforms[i] = new[] {meshFilter.transform};
;				_meshes[i] = meshFilter.sharedMesh;
				_textures[i] = meshFilter.GetComponent<MeshRenderer>().sharedMaterial.mainTexture as Texture2D;
			}
		}

		public Voxels Voxelize() {
			var meshData = Mesh.AcquireReadOnlyMeshData(_meshes);
			var satVoxels = new NativeArray<SatVoxel>(_box.Count, Allocator.TempJob);
			var createVoxelsHandle = CreateSatVoxels(meshData, satVoxels);
			
			var weightedVoxels = new NativeArray<WeightedVoxel>(_box.Count, Allocator.TempJob);
			var calculateVoxelWeightsHandle = CalculateVoxelWeights(satVoxels, weightedVoxels, createVoxelsHandle);
			
			var voxelColors = new NativeArray<byte3>(_box.Count, Allocator.TempJob);
			var voxelColored = new NativeArray<bool>(_box.Count, Allocator.TempJob);
			var readVoxelColorsHandle = ReadVoxelColors(voxelColors, voxelColored, meshData, weightedVoxels, calculateVoxelWeightsHandle);
			
			var voxelBones = new NativeArray<int>(_box.Count, Allocator.TempJob);
			var maxBoneIndex = new NativeArray<int>(1, Allocator.TempJob);
			var boneWeights = new NativeHashMap<int, float>(255 * 3, Allocator.TempJob);
			var readVoxelBonesHandle = ReadVoxelBones(voxelBones, meshData, weightedVoxels, boneWeights, maxBoneIndex,
				calculateVoxelWeightsHandle);
			
			var outerVoxels = new NativeArray<bool>(_box.Count, Allocator.TempJob);
			var outerVoxelsQueue = new NativeQueue<int3>(Allocator.TempJob);
			var findOuterVoxelsHandle = FindOuterVoxels(outerVoxels, weightedVoxels, outerVoxelsQueue, calculateVoxelWeightsHandle);
			
			JobHandle.CombineDependencies(readVoxelColorsHandle, readVoxelBonesHandle, findOuterVoxelsHandle).Complete();

			var boundsByBone =
				new NativeHashMap<int2, VoxelsBounds>(maxBoneIndex[0] * meshData.Length, Allocator.TempJob);
			var multiBoneVoxels = new NativeQueue<int>(Allocator.TempJob);
			var neighbourBones = new NativeHashMap<int2, int>(6, Allocator.TempJob);
			var innerBonesJobHandle = FillInnerVoxelBone(weightedVoxels, voxelBones, outerVoxels, boundsByBone,
				multiBoneVoxels, neighbourBones, default);

			var borderVoxelsQueue = new NativeQueue<int>(Allocator.TempJob);
			var fillInnerBorderVoxelColorJobHandle = FillInnerBorderVoxelColor(weightedVoxels, outerVoxels, voxelBones,
				voxelColored, voxelColors, borderVoxelsQueue, innerBonesJobHandle);

			var innerColors = new NativeArray<byte3>(new[] {new byte3(255, 0, 0)}, Allocator.TempJob);
			var fillInnerVoxelColorJobHandle = FillInnerVoxelColor(outerVoxels, voxelColored, innerColors, voxelColors,
				fillInnerBorderVoxelColorJobHandle);

			var compressedBones =
				new NativeHashMap<int2, int>(maxBoneIndex[0] * meshData.Length, Allocator.TempJob);
			var compressBonesJobHandle = CompressBones(weightedVoxels, outerVoxels, voxelBones, compressedBones, fillInnerVoxelColorJobHandle);
			
			var compressedVoxelsPositions = new NativeList<byte3>(_box.Count, Allocator.TempJob);
			var compressedVoxelsBones = new NativeList<byte>(_box.Count, Allocator.TempJob);
			var compressedVoxelsColors = new NativeList<byte3>(_box.Count, Allocator.TempJob);
			var compressVoxelsJobHandle = CompressVoxels(outerVoxels, voxelBones, voxelColors, 
				compressedVoxelsPositions, compressedVoxelsBones,
				compressedVoxelsColors, compressBonesJobHandle);
			compressVoxelsJobHandle.Complete();

			var voxelsAnimation = BakeAnimation(compressedBones);
			
			var voxels = Voxels.Create(_box, _bounds.min, _voxelSize, compressedVoxelsPositions, compressedVoxelsColors,
				compressedVoxelsBones, voxelsAnimation);
				
			meshData.Dispose();
			satVoxels.Dispose();
			weightedVoxels.Dispose();
			voxelColors.Dispose();
			voxelColored.Dispose();
			voxelBones.Dispose();
			maxBoneIndex.Dispose();
			boneWeights.Dispose();
			outerVoxels.Dispose();
			outerVoxelsQueue.Dispose();
			boundsByBone.Dispose();
			multiBoneVoxels.Dispose();
			neighbourBones.Dispose();
			borderVoxelsQueue.Dispose();
			innerColors.Dispose();
			compressedBones.Dispose();
			compressedVoxelsPositions.Dispose();
			compressedVoxelsBones.Dispose();
			compressedVoxelsColors.Dispose();
			
			return voxels;
		}

		private JobHandle CreateSatVoxels(Mesh.MeshDataArray meshData, NativeArray<SatVoxel> satVoxels) {
			var meshDataPositions = new NativeArray<float3>(meshData.Length, Allocator.TempJob);
			for (var i = 0; i < meshData.Length; i++) {
				meshDataPositions[i] = _positions[i];
			}
			
			var satVoxelizerJob = new SatVoxelizerJob(_bounds.min, _box, _voxelSize, meshData, meshDataPositions, satVoxels);
			return satVoxelizerJob.Schedule();
		}

		private JobHandle CalculateVoxelWeights(NativeArray<SatVoxel> compressedVoxels, NativeArray<WeightedVoxel> weightedVoxels, JobHandle jobDependency) {
			var weightsJob = new CalculateVoxelWeightsJob(compressedVoxels, weightedVoxels);
			var batchCount = compressedVoxels.Length / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			return weightsJob.Schedule(compressedVoxels.Length, batchCount, jobDependency);
		}

		private JobHandle ReadVoxelColors(NativeArray<byte3> voxelColors, NativeArray<bool> voxelColored, Mesh.MeshDataArray meshData,
			NativeArray<WeightedVoxel> weightedVoxels, JobHandle jobDependency) {
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
				new NativeArray<Color32>(texturesSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			for (var i = 0; i < _textures.Length; i++) {
				var texture = _textures[i];
				if (texture == null) texture = whiteTexture;
				var descriptor = texturesDictionary[texture];
				textureDescriptors[i] = descriptor;
				vertexUvReaders[i] = new VertexUVReader(meshData[i]);
				if (texturesCopied[texture]) continue;
				texturesCopied[texture] = true;
				var texturePixels = texture.GetPixels32();
				NativeArray<Color32>.Copy(texturePixels, 0, textures, descriptor.StartIndex, texturePixels.Length);
			}


			var batchCount = boxSize / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var read = new ReadVoxelColorJob(weightedVoxels, vertexUvReaders, textureDescriptors, textures, voxelColors,
				voxelColored);
			return read.Schedule(boxSize, batchCount, jobDependency);
		}

		private JobHandle ReadVoxelBones(NativeArray<int> voxelBones, Mesh.MeshDataArray meshData,
			NativeArray<WeightedVoxel> weightedVoxels, NativeHashMap<int, float> boneWeights,
			NativeArray<int> maxBoneIndex, JobHandle jobDependency) {
			var boxSize = voxelBones.Length;

			var boneReaders = new NativeArray<VertexBonesReader>(meshData.Length, Allocator.TempJob);
			for (var i = 0; i < meshData.Length; i++) {
				boneReaders[i] = new VertexBonesReader(meshData[i]);
			}

			var read = new ReadVoxelBoneJob(weightedVoxels, boneReaders, boneWeights, voxelBones, maxBoneIndex);
			return read.Schedule(boxSize, jobDependency);
		}

		private JobHandle FindOuterVoxels(NativeArray<bool> outerVoxels, NativeArray<WeightedVoxel> weightedVoxels,
			NativeQueue<int3> outerVoxelsQueue,
			JobHandle jobDependency) {
			var job = new FindOuterVoxelsJob(_box, weightedVoxels, outerVoxelsQueue, outerVoxels);
			return job.Schedule(jobDependency);
		}

		private JobHandle FillInnerVoxelBone(NativeArray<WeightedVoxel> weightedVoxels,
			NativeArray<int> voxelBones, NativeArray<bool> outerVoxels, NativeHashMap<int2, VoxelsBounds> boundsByBone,
			NativeQueue<int> multiBoneVoxels, NativeHashMap<int2, int> neighbourBones, JobHandle jobDependency) {
			var job = new FillInnerVoxelBoneJob(_box, weightedVoxels, voxelBones, outerVoxels, boundsByBone,
				multiBoneVoxels, neighbourBones);
			return job.Schedule(jobDependency);
		}

		private JobHandle FillInnerBorderVoxelColor(NativeArray<WeightedVoxel> weightedVoxels,
			NativeArray<bool> outerVoxels, NativeArray<int> voxelBones, NativeArray<bool> voxelColored,
			NativeArray<byte3> voxelColors, NativeQueue<int> borderVoxels, JobHandle jobDependency) {
			var job = new FillInnerBorderVoxelColorJob(_box, weightedVoxels, outerVoxels, voxelBones, voxelColored,
				voxelColors, borderVoxels);
			return job.Schedule(jobDependency);
		}

		private JobHandle FillInnerVoxelColor(NativeArray<bool> outerVoxels, NativeArray<bool> voxelColored, NativeArray<byte3> innerColors,
			NativeArray<byte3> voxelColors, JobHandle jobDependency) {
			var job = new FillInnerVoxelColorJob(innerColors.Length, outerVoxels, voxelColored, innerColors, voxelColors,
				new Unity.Mathematics.Random((uint) Random.Range(1, int.MaxValue)));
			var batchCount = _box.Count / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			return job.Schedule(_box.Count, batchCount, jobDependency);
		}
		
		private JobHandle CompressBones(NativeArray<WeightedVoxel> weightedVoxels,
			NativeArray<bool> outerVoxels, NativeArray<int> voxelBones, NativeHashMap<int2, int> compressedBones, JobHandle jobDependency) {
			var job = new CompressBonesJob(weightedVoxels, outerVoxels, voxelBones, compressedBones);
			return job.Schedule(_box.Count, jobDependency);
		}
		
		private JobHandle CompressVoxels(NativeArray<bool> outerVoxels, NativeArray<int> voxelBones,
			NativeArray<byte3> voxelColors, NativeList<byte3> compressedPositions, NativeList<byte> compressedBones,
			NativeList<byte3> compressedColors, JobHandle jobDependency) {
			var compressJob = new CompressVoxelsJob(_box, outerVoxels, voxelBones, voxelColors, compressedPositions,
				compressedBones, compressedColors);
			return compressJob.Schedule(_box.Count, jobDependency);
		}
		
		private VoxelsAnimation BakeAnimation(NativeHashMap<int2, int> bonesMapping) {
			var bonesCount = bonesMapping.Count();
			
			var initialPositions = new float3[bonesCount];
			var initialRotations = new Quaternion[bonesCount];
			foreach (var keyValue in bonesMapping) {
				var meshIndex = keyValue.Key.x - 1;
				var meshBones = _boneTransforms[meshIndex];
				initialPositions[keyValue.Value] = meshBones[Mathf.Min(meshBones.Length - 1, keyValue.Key.y)].transform.position;
				initialRotations[keyValue.Value] = meshBones[Mathf.Min(meshBones.Length - 1, keyValue.Key.y)].transform.rotation;
			}

			var maxClipLength = 0.0f;
			var maxClipFramerate = 0.0f;

			foreach (var animation in _animations) {
				if (animation == null) continue;
				maxClipLength = Mathf.Max(maxClipLength, animation.clip.length);
				maxClipFramerate = Mathf.Max(maxClipFramerate, animation.clip.frameRate);
				animation.Play();
			}
			
			var animationLength = Mathf.RoundToInt(maxClipLength * maxClipFramerate);
			var perFrameTime = maxClipLength / animationLength;
			var sampleTime = 0.0f;
			
			var bonesPositions = new float3[bonesCount * animationLength];
			var bonesRotations = new float4[bonesCount * animationLength];

			for (var i = 0; i < animationLength; i++) {
				foreach (var animation in _animations) {
					if (animation == null) continue;
					animation[animation.clip.name].time = sampleTime;
					animation.Sample();
				}
				foreach (var keyValue in bonesMapping) {
					var meshIndex = keyValue.Key.x - 1;
					var meshBones = _boneTransforms[meshIndex];
					bonesPositions[i + animationLength * keyValue.Value] =
						(float3)meshBones[Mathf.Min(meshBones.Length - 1, keyValue.Key.y)].transform.position - initialPositions[keyValue.Value];
					var rotation = (quaternion)(meshBones[Mathf.Min(meshBones.Length - 1, keyValue.Key.y)].transform.rotation *
					                            Quaternion.Inverse(initialRotations[keyValue.Value]));
					bonesRotations[i + animationLength * keyValue.Value] = rotation.value;
				}

				sampleTime += perFrameTime;
			}
			foreach (var animation in _animations) {
				animation?.Stop();
			}
			return new VoxelsAnimation(animationLength, maxClipFramerate, bonesCount, initialPositions, bonesPositions, bonesRotations);
		}
	}
}