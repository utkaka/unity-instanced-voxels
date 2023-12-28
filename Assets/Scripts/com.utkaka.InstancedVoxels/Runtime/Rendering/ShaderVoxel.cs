using com.utkaka.InstancedVoxels.Runtime.VoxelData;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	public struct ShaderVoxel {
		public readonly uint PositionBone;
		public readonly uint Color;

		public int GetBone() => (int)(PositionBone & 255);

		public byte3 GetPosition() => new((byte) ((PositionBone & 65280) >> 8), (byte) ((PositionBone & 16711680) >> 16),
			(byte) ((PositionBone & 4278190080) >> 24));

		public ShaderVoxel(byte3 position, byte bone, byte3 color) {
			PositionBone = bone | (uint)(position.x << 8) | (uint)(position.y << 16) | (uint)(position.z << 24);
			Color = color.x | (uint)(color.y << 8) | (uint)(color.z << 16);
		}
	}
}