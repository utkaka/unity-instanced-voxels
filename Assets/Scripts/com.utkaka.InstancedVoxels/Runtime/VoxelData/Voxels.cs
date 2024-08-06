using System;
using System.Reflection;
using com.utkaka.InstancedVoxels.Runtime.VoxelData.Compression;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
	[Serializable]
	public class Voxels : ScriptableObject {
		[SerializeField]
		private VoxelsBox _box;
		[SerializeField]
		private Vector3 _startPosition;
		[SerializeField]
		private float _voxelSize;
		[SerializeField, HideInInspector]
		private byte[] _plainVoxels;
		[SerializeField, HideInInspector]
		private byte[] _combinedVoxels;
		[SerializeField]
		private VoxelsAnimation _animation;
		[SerializeField, HideInInspector]
		private bool _compressBytes;
		[SerializeField] 
		private int[] _plainVoxelsOriginalFieldsSizes;
		[SerializeField] 
		private int[] _combinedVoxelsOriginalFieldsSizes;
		[SerializeField] 
		private int[] _plainVoxelsCompressedFieldsSizes;
		[SerializeField] 
		private int[] _combinedVoxelsCompressedFieldsSizes;

		public VoxelsBox Box => _box;

		public Vector3 StartPosition => _startPosition;

		public float VoxelSize => _voxelSize;

		public byte[] PlainVoxels => !_compressBytes
			? _plainVoxels
			: DecompressBytes(_plainVoxels, typeof(VoxelPlain), _plainVoxelsOriginalFieldsSizes,
				_plainVoxelsCompressedFieldsSizes);

		public byte[] CombinedVoxels => !_compressBytes
			? _combinedVoxels
			: DecompressBytes(_combinedVoxels, typeof(VoxelCombined), _combinedVoxelsOriginalFieldsSizes,
				_combinedVoxelsCompressedFieldsSizes);

		public VoxelsAnimation Animation => _animation;

		public static Voxels Create(VoxelsBox box, Vector3 startPosition, float voxelSize, NativeArray<VoxelPlain> plainVoxels,
			NativeArray<VoxelCombined> combinedVoxels, VoxelsAnimation animation, bool compressBytes) {
			var instance = CreateInstance<Voxels>();
			instance._box = box;
			instance._startPosition = startPosition;
			instance._voxelSize = voxelSize;
			instance._compressBytes = compressBytes;

			instance._plainVoxels = ConvertNativeArrayToBytes(plainVoxels);
			instance._combinedVoxels = ConvertNativeArrayToBytes(combinedVoxels);

			if (compressBytes) {
				var compressionResult = CompressBytes(instance._plainVoxels, typeof(VoxelPlain)); 
				instance._plainVoxels = compressionResult.Item1;
				instance._plainVoxelsOriginalFieldsSizes = compressionResult.Item2;
				instance._plainVoxelsCompressedFieldsSizes = compressionResult.Item3;
				
				compressionResult = CompressBytes(instance._combinedVoxels, typeof(VoxelCombined));

				instance._combinedVoxels = compressionResult.Item1;
				instance._combinedVoxelsOriginalFieldsSizes = compressionResult.Item2;
				instance._combinedVoxelsCompressedFieldsSizes = compressionResult.Item3;
			}
			
			//Debug.Log();
			
			instance._animation = animation;

			return instance;
		}

		private static unsafe byte[] ConvertNativeArrayToBytes<T>(NativeArray<T> nativeArray) where T : unmanaged {
			var bytes = new byte[nativeArray.Length * sizeof(T)];
			var slice = new NativeSlice<T>(nativeArray).SliceConvert<byte>();
			slice.CopyTo(bytes);
			return bytes;
		}

		private static unsafe Tuple<byte[], int[], int[]> CompressBytes(byte[] bytes, Type type) {
			var instanceSize = UnsafeUtility.SizeOf(type);
			var instanceCount = bytes.Length / instanceSize;
			var arrayPointer = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(bytes, out var gcHandle); 
			var fieldsSizes = new NativeList<int>(Allocator.TempJob);
			GetTypePrimitiveFieldSizes(type, fieldsSizes);
			var fieldsCount = fieldsSizes.Length;
			var fieldsMaxValues = new NativeArray<int>(fieldsCount, Allocator.TempJob);
			var getFieldsMaxValuesJob =
				new GetFieldsMaxValuesJob(instanceSize,
					BitConverter.IsLittleEndian
						? BurstCompiler.CompileFunctionPointer<ByteArrayExtensions.ReadInt>(ByteArrayExtensions
							.ReadIntValueLittleEndian)
						: BurstCompiler.CompileFunctionPointer<ByteArrayExtensions.ReadInt>(ByteArrayExtensions
							.ReadIntValueBigEndian), arrayPointer, fieldsSizes, fieldsMaxValues);
			var handle = getFieldsMaxValuesJob.Schedule(instanceCount, default);
			
			var fieldsNewSizesValues = new NativeArray<int>(fieldsCount, Allocator.TempJob);
			var instanceNewSize = new NativeArray<int>(1, Allocator.TempJob);
			var getFieldsNewSizeJob =
				new GetFieldsNewSizeJob(fieldsSizes, fieldsMaxValues, fieldsNewSizesValues, instanceNewSize);
			handle = getFieldsNewSizeJob.Schedule(handle);

			var outputBytes = new NativeArray<byte>(instanceSize * instanceCount, Allocator.TempJob);

			var compressBytesJob = new CompressBytesJob(instanceSize, instanceNewSize, BitConverter.IsLittleEndian
					? BurstCompiler.CompileFunctionPointer<ByteArrayExtensions.CompressBytes>(
						ByteArrayExtensions
							.CompressLittleEndianBytes)
					: BurstCompiler.CompileFunctionPointer<ByteArrayExtensions.CompressBytes>(
						ByteArrayExtensions
							.CompressBigEndianBytes), fieldsSizes, fieldsNewSizesValues, arrayPointer,
				(byte*)outputBytes.GetUnsafePtr());

			handle = compressBytesJob.Schedule(instanceCount,
				instanceCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, handle);
			
			handle.Complete();

			var result = new byte[instanceNewSize[0] * instanceCount];
			NativeArray<byte>.Copy(outputBytes, result, result.Length);

			var originalFieldsSizes = new int[fieldsCount];
			NativeArray<int>.Copy(fieldsSizes, originalFieldsSizes, fieldsCount);

			var compressedFieldsSizes = new int[fieldsCount];
			NativeArray<int>.Copy(fieldsNewSizesValues, compressedFieldsSizes, fieldsCount);
			
			UnsafeUtility.ReleaseGCObject(gcHandle);
			fieldsSizes.Dispose();
			fieldsMaxValues.Dispose();
			fieldsNewSizesValues.Dispose();
			instanceNewSize.Dispose();
			outputBytes.Dispose();
			return new Tuple<byte[], int[], int[]>(result, originalFieldsSizes, compressedFieldsSizes);
		}

		private static unsafe void GetTypePrimitiveFieldSizes(Type type, NativeList<int> primitiveFieldsSizes) {
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var fieldInfo in fields) {
				if (fieldInfo.FieldType.IsPrimitive) {
					primitiveFieldsSizes.Add(UnsafeUtility.SizeOf(fieldInfo.FieldType));
				} else {
					GetTypePrimitiveFieldSizes(fieldInfo.FieldType, primitiveFieldsSizes);
				}
			}
		}

		private static unsafe byte[] DecompressBytes(byte[] bytes, Type type, int[] originalFieldsSizes, int[] compressedFieldsSizes) {
			if (bytes.Length == 0) return Array.Empty<byte>();
			var fieldsCount = originalFieldsSizes.Length;
			var instanceInputSize = 0;
			var instanceOutputSize = UnsafeUtility.SizeOf(type);
			for (var i = 0; i < fieldsCount; i++) {
				instanceInputSize += compressedFieldsSizes[i];
			}

			var instanceCount = bytes.Length / instanceInputSize;

			var fieldsInputSizes = new NativeArray<int>(compressedFieldsSizes, Allocator.TempJob);
			var fieldsOutputSizes = new NativeArray<int>(originalFieldsSizes, Allocator.TempJob);

			var result = new byte[instanceCount * instanceOutputSize];
			
			var inputArrayPointer = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(bytes, out var inputGcHandle);
			var outputArrayPointer = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(result, out var outputGcHandle);
			
			var decompressBytesJob = new DecompressBytesJob(instanceInputSize, instanceOutputSize,
				fieldsCount, 
				BitConverter.IsLittleEndian
					? BurstCompiler.CompileFunctionPointer<ByteArrayExtensions.DecompressBytes>(
						ByteArrayExtensions
							.DecompressBytesToLittleEndian)
					: BurstCompiler.CompileFunctionPointer<ByteArrayExtensions.DecompressBytes>(
						ByteArrayExtensions
							.DecompressBytesToBigEndian),
				fieldsInputSizes, fieldsOutputSizes, inputArrayPointer,
				outputArrayPointer);

			decompressBytesJob.Schedule(instanceCount,
				instanceCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount).Complete();

			
			UnsafeUtility.ReleaseGCObject(inputGcHandle);
			UnsafeUtility.ReleaseGCObject(outputGcHandle);
			fieldsInputSizes.Dispose();
			fieldsOutputSizes.Dispose();

			return result;
		}

		public void CopyFrom(Voxels voxels) {
			_box = voxels._box;
			_startPosition = voxels._startPosition;
			_voxelSize = voxels._voxelSize;
			_plainVoxels = voxels._plainVoxels;
			_combinedVoxels = voxels._combinedVoxels;
			_animation = voxels._animation;
			_compressBytes = voxels._compressBytes;
			_plainVoxelsOriginalFieldsSizes = voxels._plainVoxelsOriginalFieldsSizes;
			_plainVoxelsCompressedFieldsSizes = voxels._plainVoxelsCompressedFieldsSizes;
			_combinedVoxelsOriginalFieldsSizes = voxels._combinedVoxelsOriginalFieldsSizes;
			_combinedVoxelsCompressedFieldsSizes = voxels._combinedVoxelsCompressedFieldsSizes;
		}
	}
}