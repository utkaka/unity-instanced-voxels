using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData.Compression {
    [BurstCompile]
    public unsafe struct GetFieldsMaxValuesJob : IJobFor {
        private readonly int _instanceSize;
        private readonly FunctionPointer<ByteArrayExtensions.ReadInt> _intValueReader;
        [NativeDisableUnsafePtrRestriction]
        private readonly byte* _arrayPointer;
        [ReadOnly]
        private NativeArray<int> _fieldsSizes;
        private NativeArray<int> _fieldsMaxValues;

        public GetFieldsMaxValuesJob(int instanceSize, FunctionPointer<ByteArrayExtensions.ReadInt> intValueReader, byte* arrayPointer, NativeArray<int> fieldsSizes,
            NativeArray<int> fieldsMaxValues) {
            _arrayPointer = arrayPointer;
            _fieldsSizes = fieldsSizes;
            _fieldsMaxValues = fieldsMaxValues;
            _intValueReader = intValueReader;
            _instanceSize = instanceSize;
        }

        public void Execute(int index) {
            var instancePointer = _arrayPointer + _instanceSize * index;
            for (var i = 0; i < _fieldsSizes.Length; i++) {
                _fieldsMaxValues[i] = math.max(_fieldsMaxValues[i],
                    _intValueReader.Invoke(instancePointer, _fieldsSizes[i]));
                instancePointer += _fieldsSizes[i];
            }
        }
    }
}