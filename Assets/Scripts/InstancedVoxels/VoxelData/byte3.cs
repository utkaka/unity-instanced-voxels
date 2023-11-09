using System.Runtime.InteropServices;

namespace InstancedVoxels.VoxelData {
	[StructLayout(LayoutKind.Explicit)]
	public struct byte3 {
		[FieldOffset(0)]
		public readonly byte x;
		[FieldOffset(1)]
		public readonly byte y;
		[FieldOffset(2)]
		public readonly byte z;

		public byte3(byte x, byte y, byte z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}
}