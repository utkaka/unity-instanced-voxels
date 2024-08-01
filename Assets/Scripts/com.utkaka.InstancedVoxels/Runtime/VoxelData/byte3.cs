using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
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
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 zero() { return new byte3(0, 0, 0); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 right() { return new byte3(1, 0, 0); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 up() { return new byte3(0, 1, 0); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 forward() { return new byte3(0, 0, 1); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 operator *(byte3 lhs, int rhs) {
			return new byte3((byte) (lhs.x * rhs), (byte) (lhs.y * rhs), (byte) (lhs.z * rhs));
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 operator *(int lhs, byte3 rhs) {
			return rhs * lhs;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte3 operator +(byte3 lhs, byte3 rhs) {
			return new byte3((byte) (lhs.x + rhs.x), (byte) (lhs.y + rhs.y), (byte) (lhs.z + rhs.z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(byte3 lhs, byte3 rhs) {
			return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(byte3 lhs, byte3 rhs) {
			return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
		}
	}
}