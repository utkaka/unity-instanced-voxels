using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData.Compression {
    [BurstCompile]
    public unsafe struct CompressBytesJob : IJobParallelFor {
        private readonly int _instanceInputSize;
        [ReadOnly]
        private NativeArray<int> _instanceOutputSize;
        private readonly int _fieldsCount;
        private readonly FunctionPointer<ByteArrayExtensions.CompressBytes> _copyBytesFunction;
        [ReadOnly]
        private NativeArray<int> _fieldsInputSizes;
        [ReadOnly]
        private NativeArray<int> _fieldsOutputSizes;
        [ReadOnly, NativeDisableUnsafePtrRestriction]
        private readonly byte* _inputBytesPointer;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly byte* _outputBytesPointer;

        public CompressBytesJob(int instanceInputSize, NativeArray<int> instanceOutputSize,
            FunctionPointer<ByteArrayExtensions.CompressBytes> copyBytesFunction,
            NativeArray<int> fieldsInputSizes, NativeArray<int> fieldsOutputSizes, byte* inputBytesPointer,
            byte* outputBytesPointer) {
            _instanceInputSize = instanceInputSize;
            _instanceOutputSize = instanceOutputSize;
            _fieldsCount = fieldsInputSizes.Length;
            _copyBytesFunction = copyBytesFunction;
            _fieldsInputSizes = fieldsInputSizes;
            _fieldsOutputSizes = fieldsOutputSizes;
            _inputBytesPointer = inputBytesPointer;
            _outputBytesPointer = outputBytesPointer;
        }

        public void Execute(int index) {
            var instanceOldPointer = _inputBytesPointer + _instanceInputSize * index;
            var instanceNewPointer = _outputBytesPointer + _instanceOutputSize[0] * index;
            for (var i = 0; i < _fieldsCount; i++) {
                _copyBytesFunction.Invoke(instanceOldPointer, instanceNewPointer, _fieldsInputSizes[i], _fieldsOutputSizes[i]);
                instanceNewPointer += _fieldsOutputSizes[i];
                instanceOldPointer += _fieldsInputSizes[i];
            }
        }
    }
}