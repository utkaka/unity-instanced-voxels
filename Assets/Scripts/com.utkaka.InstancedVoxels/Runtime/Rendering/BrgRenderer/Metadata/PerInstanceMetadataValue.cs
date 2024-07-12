using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class PerInstanceMetadataValue<T> : BatchMetadataValue<T> where T : unmanaged {
        public PerInstanceMetadataValue(string shaderProperty) : base(shaderProperty) { }

        public override int GetBufferSize(int instanceCount) {
            return Size * instanceCount;
        }

        public override MetadataValue GetMetadataValue(ref int offset, int instanceCount) {
            var value = new MetadataValue {
                NameID = Id,
                Value = (uint)offset | 0x80000000
            };
            offset += instanceCount * Size;
            return value;
        }
    }
}