using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
    public struct VoxelCombined {
        public readonly int3 Position;
        public readonly int3 Size;
        public readonly int Bone;
        public readonly byte3 Color;

        public VoxelCombined(int3 position, int3 size, int bone, byte3 color) {
            Position = position;
            Size = size;
            Bone = bone;
            Color = color;
        }
    }
}