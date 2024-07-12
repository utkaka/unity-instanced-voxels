using System.Linq;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class BatchMetadata {
        private readonly BatchMetadataValue[] _metaDataValues;
        public readonly int Length;

        public BatchMetadata(params BatchMetadataValue[] metaDataValues) {
            _metaDataValues = metaDataValues;
            Length = _metaDataValues.Length;
        }

        public BatchMetadataValue GetValue(int index) {
            return _metaDataValues[index];
        }
        
        public int GetValueOffset(int index, int instanceCount) {
            var offset = 0;
            for (var i = 0; i < index; i++) {
                offset += _metaDataValues[i].GetBufferSize(instanceCount);
            }
            return offset;
        }

        public int GetBufferSize(int instanceCount) {
            return _metaDataValues.Sum(metadataValue => metadataValue.GetBufferSize(instanceCount));
        }
    }
}