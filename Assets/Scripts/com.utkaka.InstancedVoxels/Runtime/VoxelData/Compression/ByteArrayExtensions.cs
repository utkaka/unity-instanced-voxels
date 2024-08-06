using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData.Compression {
    public static unsafe class ByteArrayExtensions {
        public delegate int ReadInt(byte* streamPointer, int actualSize);
        public delegate void CompressBytes(byte* inputPointer, byte* outputPointer, int stride, int count);
        public delegate void DecompressBytes(byte* inputPointer, byte* outputPointer, int stride, int count);
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ReadInt))]
        public static int ReadIntValueBigEndian(byte* s, int actualSize) {
            var result = 0;
            for (var i = 0; i < actualSize; i++) {
                result <<= 8;
                result |= 255 & *(s + i);
            }
            return result;
        }
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ReadInt))]
        public static int ReadIntValueLittleEndian(byte* s, int actualSize) {
            var result = 0;
            for (var i = actualSize - 1; i >= 0 ; i--) {
                result <<= 8;
                result |= 255 & *(s + i);
            }
            return result;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(CompressBytes))]
        public static void CompressLittleEndianBytes(byte* inputPointer, byte* outputPointer, int stride, int count) {
            UnsafeUtility.MemCpy(outputPointer, inputPointer, count);
        }
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(CompressBytes))]
        public static void CompressBigEndianBytes(byte* inputPointer, byte* outputPointer, int stride, int count) {
            for (var i = 0; i < count; i++) {
                outputPointer[i] = inputPointer[stride - 1 - i];
            }
        }
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(DecompressBytes))]
        public static void DecompressBytesToLittleEndian(byte* inputPointer, byte* outputPointer, int stride, int count) {
            UnsafeUtility.MemCpy(outputPointer, inputPointer, count);
        }
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(DecompressBytes))]
        public static void DecompressBytesToBigEndian(byte* inputPointer, byte* outputPointer, int stride, int count) {
            for (var i = 0; i < count; i++) {
                outputPointer[stride - 1 - i] = inputPointer[i];
            }
        }
    }
}