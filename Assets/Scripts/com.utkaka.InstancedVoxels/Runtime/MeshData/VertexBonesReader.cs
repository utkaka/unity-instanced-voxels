using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.MeshData {
	[BurstCompile]
	public struct VertexBonesReader {
		private readonly int _bonesCount;
		private readonly FunctionPointer<VertexAttributeReadFunctions.ReadInt> _bonesIndexReader;
		private readonly FunctionPointer<VertexAttributeReadFunctions.ReadFloat> _bonesWeightsReader;
		private readonly int _indexStride;
		private readonly unsafe byte* _indexPointer;
		private readonly int _indexSize;
		private readonly int _weightStride;
		private readonly unsafe byte* _weightPointer;
		private readonly int _weightSize;

		public int BonesCount => _bonesCount;
		public int IndexSize => _indexSize;
		public int WeightSize => _weightSize;

		public VertexBonesReader(Mesh.MeshData meshData) {
			_bonesIndexReader =
				VertexAttributeReadFunctions.GetIntFunctionPointer(meshData, VertexAttribute.BlendIndices);
			_bonesWeightsReader =
				VertexAttributeReadFunctions.GetFloatFunctionPointer(meshData, VertexAttribute.BlendWeight);

			if (meshData.HasVertexAttribute(VertexAttribute.BlendIndices)) {
				_bonesCount = meshData.GetVertexAttributeDimension(VertexAttribute.BlendIndices);
				var indexStream = meshData.GetVertexAttributeStream(VertexAttribute.BlendIndices);
				var indexOffset = meshData.GetVertexAttributeOffset(VertexAttribute.BlendIndices);
				_indexStride = meshData.GetVertexBufferStride(indexStream);
				_indexSize = VertexAttributeReadFunctions.GetAttributeSize(meshData, VertexAttribute.BlendIndices);
				unsafe {
					_indexPointer = (byte*) meshData.GetVertexData<byte>(indexStream).GetUnsafeReadOnlyPtr() +
					                indexOffset;
				}
			} else {
				_bonesCount = 0;
				_indexStride = 0;
				_indexSize = 0;
				unsafe {
					_indexPointer = (byte*) 0;
				}
			}

			if (meshData.HasVertexAttribute(VertexAttribute.BlendWeight)) {
				var weightStream = meshData.GetVertexAttributeStream(VertexAttribute.BlendWeight);
				var weightOffset = meshData.GetVertexAttributeOffset(VertexAttribute.BlendWeight);
				_weightStride = meshData.GetVertexBufferStride(weightStream);
				_weightSize = VertexAttributeReadFunctions.GetAttributeSize(meshData, VertexAttribute.BlendIndices);
				unsafe {
					_weightPointer = (byte*) meshData.GetVertexData<byte>(weightStream).GetUnsafeReadOnlyPtr() +
					                 weightOffset;
				}
			} else {
				_weightStride = 0;
				_weightSize = 0;
				unsafe {
					_weightPointer = (byte*) 0;
				}
			}
		}
		
		public unsafe int GetVertexBoneIndex(byte* pointer) => _bonesIndexReader.Invoke(pointer);
		public unsafe float GetVertexBoneWeight(byte* pointer) => _bonesWeightsReader.Invoke(pointer);

		public unsafe byte* GetVertexBoneIndexPointer(int vertexIndex) => _indexPointer + vertexIndex * _indexStride;
		public unsafe byte* GetVertexBoneWeightPointer(int vertexIndex) => _weightPointer + vertexIndex * _weightStride;
	}
}