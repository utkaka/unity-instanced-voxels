using InstancedVoxels.Voxelization.Sat;
using Unity.Mathematics;

namespace InstancedVoxels.Voxelization.Compression {
	public struct CompressedVoxel {
		public float3 VoxelCenter { get; }
		public int MeshIndex { get; }
		public int VertexIndex0 { get; }
		public int VertexIndex1 { get; }
		public int VertexIndex2 { get; }
		public float3 VertexPosition0 { get; }
		public float3 VertexPosition1 { get; }
		public float3 VertexPosition2 { get; }

		public CompressedVoxel(SatVoxel satVoxel) {
			VoxelCenter = satVoxel.VoxelCenter;
			MeshIndex = satVoxel.MeshIndex - 1;
			VertexIndex0 = satVoxel.VertexIndex0;
			VertexIndex1 = satVoxel.VertexIndex1;
			VertexIndex2 = satVoxel.VertexIndex2;
			VertexPosition0 = satVoxel.VertexPosition0;
			VertexPosition1 = satVoxel.VertexPosition1;
			VertexPosition2 = satVoxel.VertexPosition2;
		}
	}
}