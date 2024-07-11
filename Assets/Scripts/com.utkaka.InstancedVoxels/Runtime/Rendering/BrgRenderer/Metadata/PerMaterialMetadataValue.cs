using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class PerMaterialMetadataValue<T> : BatchMetadataValue<T> where T : unmanaged {
        public PerMaterialMetadataValue(string shaderProperty) : base(shaderProperty) { }
        
        public override long GetBufferSizeInFloat(uint instanceCount) {
            return SizeInFloat;
        }
        
        public override MetadataValue GetMetadataValue(ref uint offset, uint instanceCount) {
            var value = new MetadataValue {
                NameID = Id,
                Value = offset
            };
            offset += SizeInFloat;
            return value;
        }
    }
}