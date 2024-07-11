using System.Linq;
using Unity.Collections;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class BatchMetadata {
        private readonly BatchMetadataValue[] _metaDataValues;

        public int MetadataLength => _metaDataValues.Length;

        public BatchMetadata(params BatchMetadataValue[] metaDataValues) {
            _metaDataValues = metaDataValues;
        }

        public int FillMetadataValues(NativeArray<MetadataValue> metadataValues, int instanceCount) {
            var offset = 0;
            for (var i = 0; i < MetadataLength; i++) {
                metadataValues[i] = _metaDataValues[i].GetMetadataValue(ref offset, instanceCount);
            }
            return offset;
        }

        public int GetBufferSizeInFloat(int instanceCount) {
            return _metaDataValues.Sum(metadataValue => metadataValue.GetBufferSizeInFloat(instanceCount));
        }
    }
}