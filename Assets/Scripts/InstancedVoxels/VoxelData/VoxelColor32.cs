using System.Runtime.InteropServices;

namespace InstancedVoxels.VoxelData {
	[StructLayout(LayoutKind.Explicit)]
	public struct VoxelColor32 {
		[FieldOffset(0)]
		public readonly byte r;
		[FieldOffset(1)]
		public readonly byte g;
		[FieldOffset(2)]
		public readonly byte b;

		public VoxelColor32(byte r, byte g, byte b) {
			this.r = r;
			this.g = g;
			this.b = b;
		}
	}
}