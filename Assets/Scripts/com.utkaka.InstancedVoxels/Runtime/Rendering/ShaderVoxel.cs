using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	public struct ShaderVoxel {
		public readonly int3 Position;
		public readonly int Bone;
		public readonly int Color;

		public ShaderVoxel(int3 position, int bone, byte3 color) {
			Position = position;
			Bone = bone;
			Color = color.x | color.y << 8 | color.z << 16;
		}
	}
}