using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.VoxelData {
	[BurstCompile]
	[Serializable]
	public struct VoxelsBox {
		[SerializeField]
		private int3 _size;
		[SerializeField]
		private int _sizeYByZ;
		[SerializeField]
		private int _count;

		public int3 Size => _size;
		public int SizeYByZ => _sizeYByZ;
		public int Count => _count;

		public VoxelsBox(int3 size) {
			_size = size;
			_sizeYByZ = size.y * size.z;
			_count = size.x * _sizeYByZ;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetLeft(int voxelIndex) {
			return voxelIndex - _sizeYByZ;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetRight(int voxelIndex) {
			return voxelIndex + _sizeYByZ;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetBottom(int voxelIndex) {
			return voxelIndex - _size.z;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetTop(int voxelIndex) {
			return voxelIndex + _size.z;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetBack(int voxelIndex) {
			return voxelIndex - 1;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetFront(int voxelIndex) {
			return voxelIndex + 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetVoxelIndex(int3 voxelPosition) {
			return voxelPosition.x * _sizeYByZ + voxelPosition.y * _size.z + voxelPosition.z;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int3 GetVoxelPosition(int voxelIndex) {
			var z = voxelIndex % _size.z;
			voxelIndex /= _size.z;
			var y = voxelIndex % _size.y;
			var x = voxelIndex / _size.y;
			return new int3(x, y, z);
		}
	}
}