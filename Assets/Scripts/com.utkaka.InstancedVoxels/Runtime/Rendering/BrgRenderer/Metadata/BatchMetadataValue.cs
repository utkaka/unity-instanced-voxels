using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public abstract unsafe class BatchMetadataValue {
        protected readonly int Id;
        public int Size { get; protected set; }
        public abstract bool Fixed { get; }

        protected BatchMetadataValue(string shaderProperty) {
            Id = Shader.PropertyToID(shaderProperty);
        }

        protected BatchMetadataValue(string shaderProperty, int size) {
            Id = Shader.PropertyToID(shaderProperty);
            Size = size;
        }

        public abstract void SetValueToBuffer(byte* buffer, int offset);
        public abstract int GetBufferSize(int instanceCount);
        public abstract MetadataValue GetMetadataValue(ref int offset, int instanceCount);
    }
    
    public abstract unsafe class BatchMetadataValue<T> : BatchMetadataValue where T : unmanaged {
        protected BatchMetadataValue(string shaderProperty) : base(shaderProperty){
            Size = sizeof(T);
        }
    }
}