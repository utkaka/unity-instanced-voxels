using Unity.Mathematics;

namespace InstancedVoxels.Voxelization {
	public readonly struct TextureDescriptor {
		public int StartIndex { get; }
		private readonly int _width;
		private readonly int _height;

		public TextureDescriptor(int startIndex, int width, int height) {
			StartIndex = startIndex;
			_width = width;
			_height = height;
		}

		public int GetUvIndex(float2 uv) {
			var x = (int)(_width * uv.x);
			var y = (int)(_height * uv.y);
			return StartIndex + _width * y + x;
		}
	}
}