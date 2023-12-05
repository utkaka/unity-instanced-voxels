using System;
using AOT;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.utkaka.InstancedVoxels.Runtime.MeshData {
	[BurstCompile]
	public static class VertexAttributeReadFunctions {
		public unsafe delegate int ReadInt(byte* streamPointer);
		public unsafe delegate float ReadFloat(byte* streamPointer);

		public static int GetAttributeSize(Mesh.MeshData meshData, VertexAttribute vertexAttribute) {
			var format = meshData.GetVertexAttributeFormat(vertexAttribute);
			return format switch {
				VertexAttributeFormat.UInt8 => 1,
				VertexAttributeFormat.SInt8 => 1,
				VertexAttributeFormat.UInt16 => 2,
				VertexAttributeFormat.SInt16 => 2,
				VertexAttributeFormat.UInt32 => 4,
				VertexAttributeFormat.SInt32 => 4,
				VertexAttributeFormat.Float32 => 4,
				VertexAttributeFormat.Float16 => 2,
				VertexAttributeFormat.UNorm8 => 1,
				VertexAttributeFormat.SNorm8 => 1,
				VertexAttributeFormat.UNorm16 => 2,
				VertexAttributeFormat.SNorm16 => 2,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public static unsafe FunctionPointer<ReadInt> GetIntFunctionPointer(Mesh.MeshData meshData, VertexAttribute vertexAttribute) {
			if (!meshData.HasVertexAttribute(vertexAttribute))
				return BurstCompiler.CompileFunctionPointer<ReadInt>(ReadZeroInt);
			var format = meshData.GetVertexAttributeFormat(vertexAttribute);
			return format switch {
				VertexAttributeFormat.UInt8 => BurstCompiler.CompileFunctionPointer<ReadInt>(ReadIntFromUInt8),
				VertexAttributeFormat.SInt8 => BurstCompiler.CompileFunctionPointer<ReadInt>(ReadIntFromSInt8),
				VertexAttributeFormat.UInt16 => BurstCompiler.CompileFunctionPointer<ReadInt>(ReadIntFromUInt16),
				VertexAttributeFormat.SInt16 => BurstCompiler.CompileFunctionPointer<ReadInt>(ReadIntFromSInt16),
				VertexAttributeFormat.UInt32 => BurstCompiler.CompileFunctionPointer<ReadInt>(ReadIntFromUInt32),
				VertexAttributeFormat.SInt32 => BurstCompiler.CompileFunctionPointer<ReadInt>(ReadIntFromSInt32),
				_ => throw new ArgumentOutOfRangeException()
			};
		}
		
		public static unsafe FunctionPointer<ReadFloat> GetFloatFunctionPointer(Mesh.MeshData meshData, VertexAttribute vertexAttribute) {
			if (!meshData.HasVertexAttribute(vertexAttribute))
				return BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadZeroFloat);
			var format = meshData.GetVertexAttributeFormat(vertexAttribute);
			return format switch {
				VertexAttributeFormat.Float32 => BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadFloatFromFloat32),
				VertexAttributeFormat.Float16 => BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadFloatFromFloat16),
				VertexAttributeFormat.UNorm8 => BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadFloatFromUNorm8),
				VertexAttributeFormat.SNorm8 => BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadFloatFromSNorm8),
				VertexAttributeFormat.UNorm16 => BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadFloatFromUNorm16),
				VertexAttributeFormat.SNorm16 => BurstCompiler.CompileFunctionPointer<ReadFloat>(ReadFloatFromSNorm16),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		#region Int Functions
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadZeroInt(byte* streamPointer) => 0;

		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadIntFromUInt8(byte* streamPointer) => *streamPointer;
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadIntFromSInt8(byte* streamPointer) => *(sbyte*)streamPointer;

		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadIntFromUInt16(byte* streamPointer) => *(ushort*) streamPointer;
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadIntFromSInt16(byte* streamPointer) => *(short*)streamPointer;

		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadIntFromUInt32(byte* streamPointer) => (int)*(uint*)streamPointer;

		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadInt))]
		public static unsafe int ReadIntFromSInt32(byte* streamPointer) => *(int*) streamPointer;

		#endregion

		#region Float Functions
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadZeroFloat(byte* streamPointer) => 0.0f;

		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadFloatFromFloat32(byte* streamPointer) => *(float*)streamPointer;
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadFloatFromFloat16(byte* streamPointer) => *(half*)streamPointer;
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadFloatFromUNorm8(byte* streamPointer) => *streamPointer / (float)byte.MaxValue;
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadFloatFromSNorm8(byte* streamPointer) {
			var value = *(sbyte*)streamPointer;
			return math.select(value / (float)sbyte.MinValue, value / (float)sbyte.MaxValue, value > 0);
		}
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadFloatFromUNorm16(byte* streamPointer) => *(ushort*)streamPointer / (float)ushort.MaxValue;
		
		[BurstCompile]
		[MonoPInvokeCallback(typeof(ReadFloat))]
		public static unsafe float ReadFloatFromSNorm16(byte* streamPointer) {
			var value = *(short*)streamPointer;
			return math.select(value / (float)short.MinValue, value / (float)short.MaxValue, value > 0);
		}

		#endregion
	}
}