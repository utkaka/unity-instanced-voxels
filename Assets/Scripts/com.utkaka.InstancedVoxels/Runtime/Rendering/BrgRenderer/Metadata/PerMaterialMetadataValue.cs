using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class PerMaterialMetadataValue<T> : BatchMetadataValue<T> where T : unmanaged {
        public PerMaterialMetadataValue(string shaderProperty) : base(shaderProperty) { }
        
        public override int GetBufferSize(int instanceCount) {
            return Size;
        }
        
        public override MetadataValue GetMetadataValue(ref int offset, int instanceCount) {
            var value = new MetadataValue {
                NameID = Id,
                Value = (uint)offset
            };
            offset += Size;
            return value;
        }
    }
}