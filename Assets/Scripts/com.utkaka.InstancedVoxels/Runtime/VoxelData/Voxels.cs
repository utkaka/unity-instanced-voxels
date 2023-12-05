using System;
using Unity.Collections;
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
		private byte[] _indices;
		[SerializeField, HideInInspector]
		private byte[] _colors;
		[SerializeField, HideInInspector]
		private byte[] _bones;
		[SerializeField]
		private VoxelsAnimation _animation;

		public VoxelsBox Box => _box;

		public Vector3 StartPosition => _startPosition;

		public float VoxelSize => _voxelSize;

		public byte[] Indices => _indices;

		public byte[] Colors => _colors;
		
		public byte[] Bones => _bones;

		public VoxelsAnimation Animation => _animation;

		public static Voxels Create(VoxelsBox box, Vector3 startPosition, float voxelSize, NativeArray<byte3> positions,
			NativeArray<byte3> colors, NativeArray<byte> bones, VoxelsAnimation animation) {
			var instance = CreateInstance<Voxels>();
			instance._box = box;
			instance._startPosition = startPosition;
			instance._voxelSize = voxelSize;

			var indicesBytes = new byte[positions.Length * sizeof(byte) * 3];
			var indexSlice = new NativeSlice<byte3>(positions).SliceConvert<byte>();
			indexSlice.CopyTo(indicesBytes);
			instance._indices = indicesBytes;

			var colorsBytes = new byte[colors.Length * sizeof(byte) * 3];
			var colorSlice = new NativeSlice<byte3>(colors).SliceConvert<byte>();
			colorSlice.CopyTo(colorsBytes);
			instance._colors = colorsBytes;
			
			instance._bones = bones.ToArray();
			
			instance._animation = animation;

			return instance;
		}

		public void CopyFrom(Voxels voxels) {
			_box = voxels._box;
			_startPosition = voxels._startPosition;
			_voxelSize = voxels._voxelSize;
			_indices = voxels._indices;
			_colors = voxels._colors;
			_bones = voxels._bones;
			_animation = voxels._animation;
		}
	}
}