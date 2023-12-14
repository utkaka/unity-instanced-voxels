using System.Runtime.CompilerServices;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering {
	[BurstCompile]
	public static class ColorUtils {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 GammaToLinearSpace(byte3 sRGB) {
			var sRGBFloat = new float3(sRGB.x, sRGB.y, sRGB.z) / 255.0f;
			// Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
			return sRGBFloat * (sRGBFloat * (sRGBFloat * 0.305306011f + 0.682171111f) + 0.012522878f);
		}
	}
}