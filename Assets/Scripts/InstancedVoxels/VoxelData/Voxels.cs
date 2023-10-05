using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.VoxelData {
	[Serializable]
	public class Voxels : ScriptableObject {
		[SerializeField]
		private int3 _boxSize;
		[SerializeField]
		private Vector3 _startPosition;
		[SerializeField]
		private float _voxelSize;
		[SerializeField, HideInInspector]
		private byte[] _indices;
		[SerializeField, HideInInspector]
		private byte[] _colors;

		public int3 BoxSize => _boxSize;

		public Vector3 StartPosition => _startPosition;

		public float VoxelSize => _voxelSize;

		public byte[] Indices => _indices;

		public byte[] Colors => _colors;
		
		public static Voxels Create(int3 boxSize, Vector3 startPosition, float voxelSize, NativeArray<int> indices, NativeArray<float3> colors) {
			var instance = CreateInstance<Voxels>();
			instance._boxSize = boxSize;
			instance._startPosition = startPosition;
			instance._voxelSize = voxelSize;
			
			var indicesBytes = new byte[indices.Length * sizeof(int)];
			var indexSlice  = new NativeSlice<int>(indices).SliceConvert<byte>();
			indexSlice.CopyTo(indicesBytes);
			instance._indices = indicesBytes;
			
			var colorsBytes = new byte[indices.Length * sizeof(float) * 3];
			var colorSlice  = new NativeSlice<float3>(colors).SliceConvert<byte>();
			colorSlice.CopyTo(colorsBytes);
			instance._colors = colorsBytes;
			return instance;
		}
	}
}