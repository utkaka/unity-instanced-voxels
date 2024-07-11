using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class PerInstanceMetadataValue<T> : BatchMetadataValue<T> where T : unmanaged {
        public PerInstanceMetadataValue(string shaderProperty) : base(shaderProperty) { }

        public override long GetBufferSizeInFloat(uint instanceCount) {
            return SizeInFloat * instanceCount;
        }

        public override MetadataValue GetMetadataValue(ref uint offset, uint instanceCount) {
            var value = new MetadataValue {
                NameID = Id,
                Value = offset | 0x80000000
            };
            offset += instanceCount * SizeInFloat;
            return value;
        }
    }
}