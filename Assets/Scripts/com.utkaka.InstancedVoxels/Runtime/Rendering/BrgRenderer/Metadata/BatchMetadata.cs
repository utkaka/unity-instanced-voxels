using System.Linq;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Rendering.BrgRenderer.Metadata {
    public class BatchMetadata {
        public readonly int Length;
        public readonly int ItemsPerWindow;

        private readonly int _itemSize;
        private readonly int _fixedSize;
        private readonly BatchMetadataValue[] _metaDataValues;

        public BatchMetadata(params BatchMetadataValue[] metaDataValues) {
            _metaDataValues = metaDataValues;
            Length = _metaDataValues.Length;
            foreach (var batchMetadataValue in metaDataValues) {
                if (batchMetadataValue.Fixed) {
                    _fixedSize += batchMetadataValue.Size;
                } else {
                    _itemSize += batchMetadataValue.Size;
                }
            }

            if (BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer) {
                var windowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize() - _fixedSize;
                ItemsPerWindow = windowSize / _itemSize;
            } else {
                ItemsPerWindow = -1;
            }
        }

        public int GetWindowsCount(int instanceCount) {
            if (ItemsPerWindow < 0) return 1;
            return (instanceCount + ItemsPerWindow - 1) / ItemsPerWindow;
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
            return ItemsPerWindow < 0
                ? _itemSize * instanceCount + _fixedSize
                : BatchRendererGroup.GetConstantBufferMaxWindowSize() * GetWindowsCount(instanceCount);
        }
    }
}