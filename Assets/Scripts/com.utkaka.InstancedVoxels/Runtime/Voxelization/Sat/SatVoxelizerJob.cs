using System.Runtime.CompilerServices;
using com.utkaka.InstancedVoxels.Runtime.MeshData;
using com.utkaka.InstancedVoxels.Runtime.VoxelData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.Voxelization.Sat {
	[BurstCompile]
	public struct SatVoxelizerJob : IJob {
		private readonly double3 _boundsMin;
		private readonly VoxelsBox _box;
		private readonly double _voxelSize;
		private readonly double _halfVoxelSize;
		private readonly double3 _halfVoxel;
		[ReadOnly]
		private Mesh.MeshDataArray _meshDataArray;
		[ReadOnly, DeallocateOnJobCompletion]
		private readonly NativeArray<float3> _meshPositions;
		private NativeArray<SatVoxel> _voxels;

		public SatVoxelizerJob(float3 boundsMin, VoxelsBox box, float voxelSize, Mesh.MeshDataArray meshDataArray, NativeArray<float3> meshPositions, NativeArray<SatVoxel> voxels) : this() {
			_boundsMin = boundsMin;
			_box = box;
			_voxelSize = voxelSize;
			_halfVoxelSize = _voxelSize / 2.0f;
			_halfVoxel = new double3(_halfVoxelSize, _halfVoxelSize, _halfVoxelSize);
			_meshDataArray = meshDataArray;
			_meshPositions = meshPositions;
			_voxels = voxels;
		}

		public void Execute() {
			for (var index = 0; index < _meshDataArray.Length; index++) {
				var meshData = _meshDataArray[index];
				var meshPosition = _meshPositions[index];
				var positionReader = new VertexPositionReader(meshData);
				if (meshData.indexFormat == IndexFormat.UInt16) {
					var indexData = meshData.GetIndexData<short>();
					for (var j = 0; j < meshData.subMeshCount; j++) {
						var subMeshDescriptor = meshData.GetSubMesh(j);
						//TODO: Check topology
						for (var s = subMeshDescriptor.indexStart; s < subMeshDescriptor.indexStart + subMeshDescriptor.indexCount; s += 3) {
							var i0 = indexData[s];
							var i1 = indexData[s + 1];
							var i2 = indexData[s + 2];
							var p0 = positionReader.GetVertexPosition(i0) + meshPosition;
							var p1 = positionReader.GetVertexPosition(i1) + meshPosition;
							var p2 = positionReader.GetVertexPosition(i2) + meshPosition;
							ProcessTriangle(index, i0, i1, i2, p0, p1, p2);
						}
					}
				} else {
					var indexData = meshData.GetIndexData<int>();
					for (var j = 0; j < meshData.subMeshCount; j++) {
						var subMeshDescriptor = meshData.GetSubMesh(j);
						//TODO: Check topology
						for (var s = subMeshDescriptor.indexStart; s < subMeshDescriptor.indexStart + subMeshDescriptor.indexCount; s += 3) {
							var i0 = indexData[s];
							var i1 = indexData[s + 1];
							var i2 = indexData[s + 2];
							var p0 = positionReader.GetVertexPosition(i0) + meshPosition;
							var p1 = positionReader.GetVertexPosition(i1) + meshPosition;
							var p2 = positionReader.GetVertexPosition(i2) + meshPosition;
							ProcessTriangle(index, i0, i1, i2, p0, p1, p2);
						}
					}
				}
			}
		}

		private void ProcessTriangle(int meshIndex, int i0, int i1, int i2, double3 p0, double3 p1, double3 p2) {
			var cell0 = GetVertexPosition(p0 - _boundsMin, _voxelSize);
			var cell1 = GetVertexPosition(p1 - _boundsMin, _voxelSize);
			var cell2 = GetVertexPosition(p2 - _boundsMin, _voxelSize);

			var minCell = new int3(math.min(cell0.x, math.min(cell1.x, cell2.x)),
				math.min(cell0.y, math.min(cell1.y, cell2.y)),
				math.min(cell0.z, math.min(cell1.z, cell2.z)));
			var maxCell = new int3(math.max(cell0.x, math.max(cell1.x, cell2.x)),
				math.max(cell0.y, math.max(cell1.y, cell2.y)),
				math.max(cell0.z, math.max(cell1.z, cell2.z)));
						
			for (var k = minCell.x; k <= maxCell.x; k++) {
				for (var m = minCell.y; m <= maxCell.y; m++) {
					for (var n = minCell.z; n <= maxCell.z; n++) {
						var voxelCenter = new double3(_voxelSize * k, _voxelSize * m, _voxelSize * n) +
						                  _boundsMin + _halfVoxel;
						var voxelIndex = (k + 1) * _box.SizeYByZ + (m + 1) * _box.Size.z + n + 1;
						var existingVoxel = _voxels[voxelIndex];
						var d0 = math.distancesq(p0, voxelCenter);;
						var d1 = math.distancesq(p1, voxelCenter);;
						var d2 = math.distancesq(p2, voxelCenter);;
						var distance = d0 + d1 + d2;
						if ((existingVoxel.MeshIndex == 0 || distance < existingVoxel.Distance) && SatTriangleIntersectsCube(p0, p1, p2, voxelCenter)) {
							_voxels[voxelIndex] = new SatVoxel((float3)voxelCenter, (float)distance, meshIndex + 1, i0, i1, i2, (float3)p0, (float3)p1, (float3)p2);
						}
					}
				}
			}
		}
		
		private bool SatTriangleIntersectsCube(double3 v0, double3 v1, double3 v2, double3 c) {
			v0 -= c;
			v1 -= c;
			v2 -= c;
			var f0 = v1 - v0;
			var f1 = v2 - v1;
			var f2 = v0 - v2;

			if (!SatTestAxis(v0, v1, v2, math.cross(math.right(), f0))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.right(), f1))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.right(), f2))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.up(), f0))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.up(), f1))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.up(), f2))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.forward(), f0))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.forward(), f1))) return false;
			if (!SatTestAxis(v0, v1, v2, math.cross(math.forward(), f2))) return false;
		    
			if (!SatTestAxis(v0, v1, v2, math.right())) return false;
			if (!SatTestAxis(v0, v1, v2, math.up())) return false;
			if (!SatTestAxis(v0, v1, v2, math.forward())) return false;
		    
			var triangleNormal = math.cross(f0, f1);
			return SatTestAxis(v0, v1, v2, triangleNormal);
		}
		
		private bool SatTestAxis(double3 v0, double3 v1, double3 v2, double3 axis) {
			var p0 = math.dot(v0, axis);
			var p1 = math.dot(v1, axis);
			var p2 = math.dot(v2, axis);
			var r = _halfVoxelSize * math.abs(math.dot(math.right(), axis)) +
			        _halfVoxelSize * math.abs(math.dot(math.up(), axis)) +
			        _halfVoxelSize * math.abs(math.dot(math.forward(), axis));
			return !(math.max(-math.max(p0, math.max(p1, p2)), math.min(p0, math.min(p1, p2))) > r);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int3 GetVertexPosition(double3 position, double voxelSize) {
			return new int3((int)(position.x / voxelSize),
				(int)(position.y / voxelSize), (int)(position.z / voxelSize));
		}
	}
}