using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.MeshData {
	public readonly struct VertexAttributeReader {
		private readonly int _streamStride;
		private readonly unsafe byte* _streamPointer;

		public VertexAttributeReader(Mesh.MeshData meshData, VertexAttribute vertexAttribute) {
			var attributeStream = meshData.GetVertexAttributeStream(vertexAttribute);
			var attributeOffset = meshData.GetVertexAttributeOffset(vertexAttribute);
			_streamStride = meshData.GetVertexBufferStride(attributeStream);
			unsafe {
				_streamPointer = (byte*) meshData.GetVertexData<byte>(attributeStream).GetUnsafeReadOnlyPtr() + attributeOffset;	
			}
		}

		public unsafe T GetVertexAttribute<T>(int index) where T : unmanaged {
			var vertexPointer = _streamPointer + index * _streamStride;
			return *(T*)vertexPointer;
		}
	}
}