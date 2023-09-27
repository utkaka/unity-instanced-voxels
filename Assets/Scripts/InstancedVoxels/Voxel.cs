using System;
using UnityEngine;

namespace InstancedVoxels {
	[Serializable]
	public struct Voxel {
		[SerializeField]
		private int _index;
		[SerializeField]
		private int _color;
		[SerializeField]
		private int _bone;

		public int Index => _index;
		public int Color => _color;
		public int Bone => _bone;

		public Voxel(int index, int color, int bone) {
			_index = index;
			_color = color;
			_bone = bone;
		}
	}
}