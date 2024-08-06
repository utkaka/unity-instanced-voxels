using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData.Compression {
    [BurstCompile]
    public unsafe struct DecompressBytesJob : IJobParallelFor {
        private readonly int _instanceInputSize;
        private readonly int _instanceOutputSize;
        private readonly int _fieldsCount;
        private readonly FunctionPointer<ByteArrayExtensions.DecompressBytes> _copyBytesFunction;
        [ReadOnly]
        private NativeArray<int> _fieldsInputSizes;
        [ReadOnly]
        private NativeArray<int> _fieldsOutputSizes;
        [ReadOnly, NativeDisableUnsafePtrRestriction]
        private readonly byte* _inputBytesPointer;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private readonly byte* _outputBytesPointer;

        public DecompressBytesJob(int instanceInputSize, int instanceOutputSize, int fieldsCount, FunctionPointer<ByteArrayExtensions.DecompressBytes> copyBytesFunction, NativeArray<int> fieldsInputSizes, NativeArray<int> fieldsOutputSizes, byte* inputBytesPointer, byte* outputBytesPointer) {
            _instanceInputSize = instanceInputSize;
            _instanceOutputSize = instanceOutputSize;
            _fieldsCount = fieldsCount;
            _copyBytesFunction = copyBytesFunction;
            _fieldsInputSizes = fieldsInputSizes;
            _fieldsOutputSizes = fieldsOutputSizes;
            _inputBytesPointer = inputBytesPointer;
            _outputBytesPointer = outputBytesPointer;
        }

        public void Execute(int index) {
            var instanceInputPointer = _inputBytesPointer + _instanceInputSize * index;
            var instanceOutputPointer = _outputBytesPointer + _instanceOutputSize * index;
            for (var i = 0; i < _fieldsCount; i++) {
                _copyBytesFunction.Invoke(instanceInputPointer, instanceOutputPointer, _fieldsOutputSizes[i], _fieldsInputSizes[i]);
                instanceOutputPointer += _fieldsOutputSizes[i];
                instanceInputPointer += _fieldsInputSizes[i];
            }
        }
    }
}