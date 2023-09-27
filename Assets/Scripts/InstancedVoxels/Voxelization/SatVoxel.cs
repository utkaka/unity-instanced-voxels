using Unity.Mathematics;

namespace InstancedVoxels.Voxelization {
	public struct SatVoxel {
		public float3 VoxelCenter { get; }
		public float Distance { get; }
		public int MeshIndex { get; }
		public int VertexIndex0 { get; }
		public int VertexIndex1 { get; }
		public int VertexIndex2 { get; }
		
		public float3 VertexPosition0 { get; }
		public float3 VertexPosition1 { get; }
		public float3 VertexPosition2 { get; }


		public SatVoxel(float3 voxelCenter, float distance, int meshIndex, int vertexIndex0, int vertexIndex1, int vertexIndex2, float3 vertexPosition0, float3 vertexPosition1, float3 vertexPosition2) {
			VoxelCenter = voxelCenter;
			Distance = distance;
			MeshIndex = meshIndex;
			VertexIndex0 = vertexIndex0;
			VertexIndex1 = vertexIndex1;
			VertexIndex2 = vertexIndex2;
			VertexPosition0 = vertexPosition0;
			VertexPosition1 = vertexPosition1;
			VertexPosition2 = vertexPosition2;
		}
	}
}