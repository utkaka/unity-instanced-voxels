using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public abstract class BatchMetadataValue {
        protected readonly int Id;
        protected uint SizeInFloat;

        protected BatchMetadataValue(string shaderProperty) {
            Id = Shader.PropertyToID(shaderProperty);
        }

        protected BatchMetadataValue(string shaderProperty, uint sizeInFloat) {
            Id = Shader.PropertyToID(shaderProperty);
            SizeInFloat = sizeInFloat;
        }

        public abstract long GetBufferSizeInFloat(uint instanceCount);
        public abstract MetadataValue GetMetadataValue(ref uint offset, uint instanceCount);
    }
    
    public abstract unsafe class BatchMetadataValue<T> : BatchMetadataValue where T : unmanaged {
        protected BatchMetadataValue(string shaderProperty) : base(shaderProperty){
            SizeInFloat = (uint)sizeof(T);
        }
    }
}