using com.utkaka.InstancedVoxels.Runtime.Voxelization.Sat;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.Weights {
	public struct WeightedVoxel {
		public int MeshIndex { get; }
		public int VertexIndex0 { get; }
		public int VertexIndex1 { get; }
		public int VertexIndex2 { get; }
		
		public float VertexWeight0 { get; }
		public float VertexWeight1 { get; }
		public float VertexWeight2 { get; }

		public WeightedVoxel(SatVoxel satVoxel, float vertexWeight0, float vertexWeight1, float vertexWeight2) {
			MeshIndex = satVoxel.MeshIndex;
			VertexIndex0 = satVoxel.VertexIndex0;
			VertexIndex1 = satVoxel.VertexIndex1;
			VertexIndex2 = satVoxel.VertexIndex2;
			VertexWeight0 = vertexWeight0;
			VertexWeight1 = vertexWeight1;
			VertexWeight2 = vertexWeight2;
		}
		
		public WeightedVoxel(WeightedVoxel weightedVoxel, int meshIndex) {
			MeshIndex = meshIndex;
			VertexIndex0 = weightedVoxel.VertexIndex0;
			VertexIndex1 = weightedVoxel.VertexIndex1;
			VertexIndex2 = weightedVoxel.VertexIndex2;
			VertexWeight0 = weightedVoxel.VertexWeight0;
			VertexWeight1 = weightedVoxel.VertexWeight1;
			VertexWeight2 = weightedVoxel.VertexWeight2;
		}
	}
}