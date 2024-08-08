using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	public struct ShaderVoxel {
		public readonly int3 Position1;
		public readonly int3 Size;
		public readonly int Bone;
		public readonly int Color;

		public ShaderVoxel(int3 position, int3 size, int bone, byte3 color) {
			Position1 = position;
			Size = size;
			Bone = bone;
			Color = color.x | color.y << 8 | color.z << 16;
		}
	}
}