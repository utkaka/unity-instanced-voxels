using com.utkaka.InstancedVoxels.Runtime.VoxelData;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	public struct ShaderVoxel {
		public readonly int PositionBone;
		public readonly int Color;

		public int GetBone() => PositionBone & 255;

		public byte3 GetPosition() => new((byte) ((PositionBone & 65280) >> 8), (byte) ((PositionBone & 16711680) >> 16),
			(byte) ((PositionBone & 4278190080) >> 24));

		public ShaderVoxel(byte3 position, byte bone, byte3 color) {
			PositionBone = bone | position.x << 8 | position.y << 16 | position.z << 24;
			Color = color.x | color.y << 8 | color.z << 16;
		}
	}
}