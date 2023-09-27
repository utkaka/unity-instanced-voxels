using Unity.Mathematics;
using UnityEngine;

namespace InstancedVoxels {
	public class ScriptableVoxels : ScriptableObject {
		[SerializeField]
		private int3 _boxSize;
		[SerializeField]
		private Vector3 _startPosition;
		[SerializeField]
		private float _voxelSize;
		[SerializeField, HideInInspector]
		private Color[] _colors;

		public int3 BoxSize => _boxSize;

		public Vector3 StartPosition => _startPosition;

		public float VoxelSize => _voxelSize;

		public Color[] Colors => _colors;

		public static ScriptableVoxels Create(float voxelSize, Vector3 startPosition, int3 boxSize, Color[] colors) {
			var instance = CreateInstance<ScriptableVoxels>();
			instance._voxelSize = voxelSize;
			instance._startPosition = startPosition;
			instance._boxSize = boxSize;
			instance._colors = colors;
			return instance;
		}
	}
}