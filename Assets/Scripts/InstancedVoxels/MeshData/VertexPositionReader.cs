using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace InstancedVoxels.MeshData {
	public readonly struct VertexPositionReader {
		private readonly VertexAttributeReader _attributeReader;
		
		public VertexPositionReader(Mesh.MeshData meshData) {
			_attributeReader = new VertexAttributeReader(meshData, VertexAttribute.Position);
		}

		public float3 GetVertexPosition(int index) {
			return _attributeReader.GetVertexAttribute<float3>(index);
		}
	}
}