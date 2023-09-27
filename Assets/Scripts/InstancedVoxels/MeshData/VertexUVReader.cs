using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace InstancedVoxels.MeshData {
	public readonly struct VertexUVReader {
		private readonly VertexAttributeReader _attributeReader;
		
		public VertexUVReader(Mesh.MeshData meshData) {
			_attributeReader = new VertexAttributeReader(meshData, VertexAttribute.TexCoord0);
		}

		public float2 GetVertexUV(int index) {
			return _attributeReader.GetVertexAttribute<float2>(index);
		}
	}
}