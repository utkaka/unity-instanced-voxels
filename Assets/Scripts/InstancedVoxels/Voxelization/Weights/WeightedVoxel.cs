using InstancedVoxels.Voxelization.Compression;

namespace InstancedVoxels.Voxelization.Weights {
	public struct WeightedVoxel {
		public int MeshIndex { get; }
		public int VertexIndex0 { get; }
		public int VertexIndex1 { get; }
		public int VertexIndex2 { get; }
		
		public float VertexWeight0 { get; }
		public float VertexWeight1 { get; }
		public float VertexWeight2 { get; }

		public WeightedVoxel(CompressedVoxel compressedVoxel, float vertexWeight0, float vertexWeight1, float vertexWeight2) {
			MeshIndex = compressedVoxel.MeshIndex;
			VertexIndex0 = compressedVoxel.VertexIndex0;
			VertexIndex1 = compressedVoxel.VertexIndex1;
			VertexIndex2 = compressedVoxel.VertexIndex2;
			VertexWeight0 = vertexWeight0;
			VertexWeight1 = vertexWeight1;
			VertexWeight2 = vertexWeight2;
		}
	}
}