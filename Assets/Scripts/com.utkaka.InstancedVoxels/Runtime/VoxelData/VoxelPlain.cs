using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
    public struct VoxelPlain {
        public readonly int3 Position;
        public readonly int Bone;
        public readonly byte3 Color;

        public VoxelPlain(int3 position, int bone, byte3 color) {
            Position = position;
            Bone = bone;
            Color = color;
        }
    }
}