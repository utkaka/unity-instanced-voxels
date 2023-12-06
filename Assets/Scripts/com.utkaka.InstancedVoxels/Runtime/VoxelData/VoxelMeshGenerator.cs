using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.VoxelData {
	public static class VoxelMeshGenerator {
		[StructLayout(LayoutKind.Sequential)]
		private struct Vertex {
			public float3 Position;
			public float3 Normal;
		}
		
		public static Mesh GetCubeMesh(float voxelSize) {
			var meshDataArray = Mesh.AllocateWritableMeshData(1);
			var meshData = meshDataArray[0];

			var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3);
			vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3);
			meshData.SetVertexBufferParams(24, vertexAttributes);
			meshData.SetIndexBufferParams(36, IndexFormat.UInt16);
			
			var vertices = meshData.GetVertexData<Vertex>();
			var triangleIndices = meshData.GetIndexData<ushort>();
			var sideTriangles = GetSideTriangles();
			
			for (var i = 0; i < 6; i++) {
				var sideVertices = GetSideVertices(i, voxelSize);
				var vertex = new Vertex {
					Normal = GetSideNormal(i),
				};
				for (var j = 0; j < 4; j++) {
					vertex.Position = sideVertices[j];
					vertices[i * 4 + j] = vertex;
				}
				for (var j = 0; j < 6; j++) {
					triangleIndices[i * 6 + j] = (ushort)(sideTriangles[j] + i * 4);
				}

				sideVertices.Dispose();
			}

			sideTriangles.Dispose();
			vertexAttributes.Dispose();

			var bounds = new Bounds(Vector3.one * (voxelSize / 2.0f), new Vector3(voxelSize, voxelSize));
			meshData.subMeshCount = 1;
			meshData.SetSubMesh(0, new SubMeshDescriptor(0, 36) {
				bounds = bounds, vertexCount = 24 }, MeshUpdateFlags.DontRecalculateBounds);
			var mesh = new Mesh {bounds = bounds, name = "Voxel Mesh"};
			Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
			
			
			
			/*var triangles = GetSideTriangles();
			var meshVertices = new Vector3[24];
			var meshTriangles = new int[36];
			
			var mesh = new Mesh {
				vertices = meshVertices,
				triangles = meshTriangles
			};
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();*/
			return mesh;
		}

		/*public static Mesh GetSideMesh(int sideIndex, float voxelSize) {
			var mesh = new Mesh {
				vertices = GetSideVertices(sideIndex, voxelSize),
				triangles = GetSideTriangles()
			};
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			return mesh;
		}*/
		
		private static NativeArray<ushort> GetSideTriangles() {
			var result = new NativeArray<ushort>(6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			result[0] = 0;
			result[1] = 2;
			result[2] = 1;
			result[3] = 1;
			result[4] = 2;
			result[5] = 3;
			return result;
		}

		/*Left = 0, Right = 1, Front = 2, Back = 3, Bottom = 4, Top = 5*/
		private static float3 GetSideNormal(int sideIndex) {
			return sideIndex switch {
				0 => math.left(),
				1 => math.right(),
				2 => math.back(),
				3 => math.forward(),
				4 => math.down(),
				5 => math.up(),
				_ => throw new NotImplementedException()
			};
		}

		private static NativeArray<float3> GetSideVertices(int sideIndex, float voxelSize) {
			var result = new NativeArray<float3>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			switch (sideIndex) {
				case 0:
					result[0] = float3.zero;
					result[1] = math.up() * voxelSize;
					result[2] = math.forward() * voxelSize;
					result[3] = (math.up() + math.forward()) * voxelSize;
					return result;
				case 1:
					result[0] = math.right() * voxelSize;
					result[1] = (math.right() + math.forward()) * voxelSize;
					result[2] = (math.right() + math.up()) * voxelSize;
					result[3] = (math.right() + math.up() + math.forward()) * voxelSize;
					return result;
				case 2:
					result[0] = float3.zero;
					result[1] = math.right() * voxelSize;
					result[2] = math.up() * voxelSize;
					result[3] = (math.right() + math.up()) * voxelSize;
					return result;
				case 3:
					result[0] = (math.right() + math.forward()) * voxelSize;
					result[1] = math.forward() * voxelSize;
					result[2] = (math.right() + math.up() + math.forward()) * voxelSize;
					result[3] = (math.up() + math.forward()) * voxelSize;
					return result;
				case 4:
					result[0] = math.forward() * voxelSize;
					result[1] = (math.right() + math.forward()) * voxelSize;
					result[2] = float3.zero;
					result[3] = math.right() * voxelSize;
					return result;
				case 5:
					result[0] = math.up() * voxelSize;
					result[1] = (math.right() + math.up()) * voxelSize;
					result[2] = (math.up() + math.forward()) * voxelSize;
					result[3] = (math.right() + math.up() + math.forward()) * voxelSize;
					return result;
			}

			throw new NotImplementedException();
		}
	}
}