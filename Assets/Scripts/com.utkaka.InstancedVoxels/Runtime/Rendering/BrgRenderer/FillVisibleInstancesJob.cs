using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer
{
    [BurstCompile]
    public unsafe struct FillVisibleInstancesJob : IJobParallelFor
    {
        [ReadOnly]
        private NativeList<int> _sideVoxelsList;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private int* _visibleInstances;
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        private int* _tempVisibleInstances;

        public FillVisibleInstancesJob(int* visibleInstances, int* tempVisibleInstances, NativeList<int> sideVoxelsList)
        {
            _sideVoxelsList = sideVoxelsList;
            _visibleInstances = visibleInstances;
            _tempVisibleInstances = tempVisibleInstances;
        }

        public void Execute(int index)
        {
            index = _sideVoxelsList[index];
            _visibleInstances[index] = index;
            _tempVisibleInstances[index] = index;
        }
    }
}