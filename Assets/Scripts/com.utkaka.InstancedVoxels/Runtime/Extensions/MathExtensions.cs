using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Extensions {
    public static class MathExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Int3Zero() { return new int3(0, 0, 0); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Int3Right() { return new int3(1, 0, 0); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Int3Up() { return new int3(0, 1, 0); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Int3Forward() { return new int3(0, 0, 1); }   
    }
}