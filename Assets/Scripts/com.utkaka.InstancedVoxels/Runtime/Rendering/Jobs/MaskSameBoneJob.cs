using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.Jobs {
	[BurstCompile]
	public struct MaskSameBoneJob : IJobParallelFor {
		private readonly VoxelsBox _voxelsBox;
		[ReadOnly]
		private NativeSlice<byte3> _inputPositions;
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<byte> _voxelBoxBones;
		[WriteOnly]
		private NativeArray<byte> _boneMasks;

		public MaskSameBoneJob(VoxelsBox voxelsBox, NativeSlice<byte3> inputPositions, NativeArray<byte> voxelBoxBones, NativeArray<byte> boneMasks) {
			_voxelsBox = voxelsBox;
			_inputPositions = inputPositions;
			_voxelBoxBones = voxelBoxBones;
			_boneMasks = boneMasks;
		}

		public void Execute(int index) {
			var voxelIndex = _voxelsBox.GetExtendedVoxelIndex(_inputPositions[index]);
			var mask = 1;
			var bone = _voxelBoxBones[voxelIndex];
			
			var neighbourIndex = _voxelsBox.GetLeft(voxelIndex);
			mask |= math.select(0, 2, bone == _voxelBoxBones[neighbourIndex]);
			neighbourIndex = _voxelsBox.GetRight(voxelIndex);
			mask |= math.select(0, 4, bone == _voxelBoxBones[neighbourIndex]);
			neighbourIndex = _voxelsBox.GetBack(voxelIndex);
			mask |= math.select(0, 8, bone == _voxelBoxBones[neighbourIndex]);
			neighbourIndex = _voxelsBox.GetFront(voxelIndex);
			mask |= math.select(0, 16, bone == _voxelBoxBones[neighbourIndex]);
			neighbourIndex = _voxelsBox.GetBottom(voxelIndex);
			mask |= math.select(0, 32, bone == _voxelBoxBones[neighbourIndex]);
			neighbourIndex = _voxelsBox.GetTop(voxelIndex);
			mask |= math.select(0, 64, bone == _voxelBoxBones[neighbourIndex]);
			
			_boneMasks[index] = (byte)mask;
		}
	}
}