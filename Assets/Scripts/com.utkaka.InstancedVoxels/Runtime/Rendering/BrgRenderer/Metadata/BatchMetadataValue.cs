using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public abstract class BatchMetadataValue {
        protected readonly int Id;
        protected int SizeInFloat;

        protected BatchMetadataValue(string shaderProperty) {
            Id = Shader.PropertyToID(shaderProperty);
        }

        protected BatchMetadataValue(string shaderProperty, int sizeInFloat) {
            Id = Shader.PropertyToID(shaderProperty);
            SizeInFloat = sizeInFloat;
        }

        public abstract int GetBufferSizeInFloat(int instanceCount);
        public abstract MetadataValue GetMetadataValue(ref int offset, int instanceCount);
    }
    
    public abstract unsafe class BatchMetadataValue<T> : BatchMetadataValue where T : unmanaged {
        protected BatchMetadataValue(string shaderProperty) : base(shaderProperty){
            SizeInFloat = sizeof(T);
        }
    }
}