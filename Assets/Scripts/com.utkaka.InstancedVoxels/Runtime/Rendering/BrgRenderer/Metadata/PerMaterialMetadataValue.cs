using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public unsafe class PerMaterialMetadataValue<T> : BatchMetadataValue<T> where T : unmanaged {
        private readonly T _value;
        public override bool Fixed => true;

        public PerMaterialMetadataValue(string shaderProperty, T value = default) : base(shaderProperty) {
            _value = value;
        }

        public override void SetValueToBuffer(byte* buffer, int offset) {
            *(T*)(buffer + offset) = _value;
        }
        
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