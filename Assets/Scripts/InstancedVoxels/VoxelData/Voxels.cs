using System;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels.VoxelData {
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

		public static Voxels Create(VoxelsBox box, Vector3 startPosition, float voxelSize, NativeArray<int> indices,
			NativeArray<VoxelColor32> colors, NativeArray<int> bones, VoxelsAnimation animation) {
			var instance = CreateInstance<Voxels>();
			instance._box = box;
			instance._startPosition = startPosition;
			instance._voxelSize = voxelSize;

			var indicesBytes = new byte[indices.Length * sizeof(int)];
			var indexSlice = new NativeSlice<int>(indices).SliceConvert<byte>();
			indexSlice.CopyTo(indicesBytes);
			instance._indices = indicesBytes;

			var colorsBytes = new byte[colors.Length * sizeof(byte) * 3];
			var colorSlice = new NativeSlice<VoxelColor32>(colors).SliceConvert<byte>();
			colorSlice.CopyTo(colorsBytes);
			instance._colors = colorsBytes;

			var bonesBytes = new byte[bones.Length * sizeof(int)];
			var bonesSlice = new NativeSlice<int>(bones).SliceConvert<byte>();
			bonesSlice.CopyTo(bonesBytes);
			instance._bones = bonesBytes;

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