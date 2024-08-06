using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData.Compression {
    [BurstCompile]
    public struct GetFieldsNewSizeJob : IJob {
        private readonly int _fieldsCount;
        [ReadOnly]
        private NativeArray<int> _fieldsMaxValues;
        private NativeArray<int> _fieldsNewSizes;
        [WriteOnly]
        private NativeArray<int> _instanceNewSize;

        public GetFieldsNewSizeJob(NativeArray<int> fieldsSizes, NativeArray<int> fieldsMaxValues,
            NativeArray<int> fieldsNewSizes, NativeArray<int> instanceNewSize) {
            _fieldsCount = fieldsSizes.Length;
            _fieldsMaxValues = fieldsMaxValues;
            _fieldsNewSizes = fieldsNewSizes;
            _instanceNewSize = instanceNewSize;
        }

        public void Execute() {
            for (var i = 0; i < _fieldsCount; i++) {
                var maxCurrentValue = 255;
                for (var j = 1; j <= 4; j++) {
                    if (_fieldsMaxValues[i] <= maxCurrentValue) {
                        _fieldsNewSizes[i] = j;
                        break;
                    }
                    maxCurrentValue = (maxCurrentValue << 8) | 255;
                }    
            }

            var instanceNewSize = 0;
            for (var i = 0; i < _fieldsCount; i++) {
                instanceNewSize += _fieldsNewSizes[i];
            }

            _instanceNewSize[0] = instanceNewSize;
        }
    }
}