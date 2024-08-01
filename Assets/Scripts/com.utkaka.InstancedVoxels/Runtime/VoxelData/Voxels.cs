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
		private byte[] _plainVoxels;
		[SerializeField, HideInInspector]
		private byte[] _compressedVoxels;
		[SerializeField]
		private VoxelsAnimation _animation;

		public VoxelsBox Box => _box;

		public Vector3 StartPosition => _startPosition;

		public float VoxelSize => _voxelSize;

		public byte[] PlainVoxels => _plainVoxels;
		public byte[] CompressedVoxels => _compressedVoxels;

		public VoxelsAnimation Animation => _animation;

		public static unsafe Voxels Create(VoxelsBox box, Vector3 startPosition, float voxelSize, NativeList<VoxelPlain> plainVoxels,
			NativeList<VoxelCompressed> compressedVoxels, VoxelsAnimation animation) {
			var instance = CreateInstance<Voxels>();
			instance._box = box;
			instance._startPosition = startPosition;
			instance._voxelSize = voxelSize;

			var plainBytes = new byte[plainVoxels.Length * sizeof(VoxelPlain)];
			var plainSlice = new NativeSlice<VoxelPlain>(plainVoxels).SliceConvert<byte>();
			plainSlice.CopyTo(plainBytes);
			instance._plainVoxels = plainBytes;

			var compressedBytes = new byte[compressedVoxels.Length * sizeof(VoxelCompressed)];
			var compressedSlice = new NativeSlice<VoxelCompressed>(compressedVoxels).SliceConvert<byte>();
			compressedSlice.CopyTo(compressedBytes);
			instance._compressedVoxels = compressedBytes;
			
			instance._animation = animation;

			return instance;
		}

		public void CopyFrom(Voxels voxels) {
			_box = voxels._box;
			_startPosition = voxels._startPosition;
			_voxelSize = voxels._voxelSize;
			_plainVoxels = voxels._plainVoxels;
			_compressedVoxels = voxels._compressedVoxels;
			_animation = voxels._animation;
		}
	}
}