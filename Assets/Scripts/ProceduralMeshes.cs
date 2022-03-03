using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

namespace ProceduralMeshes
{
    //[System.Serializable, BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    [System.Serializable]
    public struct NoiseSettings
    {
        public float3 center;
        [Range(1, 8)]
        public int numLayers;
        public float roughness;
        public float strength;
        public float persistence;
        public float baseRoughness;
        public float minValue;
    }

   
    //[System.Serializable, BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    [System.Serializable]
    public struct NoiseLayer
    {
        public bool active;
        public bool useFirstLayerAsMask;
        public NoiseSettings noiseSettings;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Passer
    {
        public NoiseLayer nl1;
        public NoiseLayer nl2;
        public NoiseLayer nl3;
        public NoiseLayer nl4;

        
    }


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<G, S> : IJobFor
        where G : struct, IMeshGenerator
        where S : struct, IMeshStreams
    {
        G generator;
        [WriteOnly]
        S streams;
        public void Execute(int i) => generator.Execute(i, streams);

        public static JobHandle ScheduleParallel (
            Mesh mesh, Mesh.MeshData meshData, int resolution, JobHandle dependency, Passer passer)
        {
            var job = new MeshJob<G, S>();
            job.generator.Resolution = resolution;
            job.generator.passer = passer;
            job.streams.Setup(
                meshData,
                mesh.bounds = job.generator.Bounds,
                job.generator.VertexCount,
                job.generator.IndexCount);
            return job.ScheduleParallel(job.generator.JobLength, 1, dependency);
        }

    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, JobHandle dependency, Passer passer);
    public struct PVertex
    {
        public float3 position, normal;
        public float4 tangent;
        public float2 texCoord0;
    }

    public interface IMeshStreams
    {
        void Setup(Mesh.MeshData data, Bounds bounds, int vertexCount, int indexCount);

        void SetVertex(int index, PVertex data);

        void SetTriangle(int index, int3 triangle);

    }

    public interface IMeshGenerator
    {
        void Execute<S>(int i, S streams) where S : struct, IMeshStreams;

        int VertexCount { get; }

        int IndexCount { get; }

        int JobLength { get; }

        Bounds Bounds { get; }

        int Resolution { get; set; }

        Passer passer { get; set; }

    }

}

namespace ProceduralMeshes.Streams
{
    public struct SingleStream : IMeshStreams
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TriangleUInt16
        {
            public ushort a, b, c;

            public static implicit operator TriangleUInt16(int3 t) => new TriangleUInt16
            {
                a = (ushort)t.x,
                b = (ushort)t.y,
                c = (ushort)t.z

            };
        }
        [StructLayout(LayoutKind.Sequential)]
        struct Stream0
        {
            public float3 position, normal;
            public float4 tangent;
            public float2 texCoord0;
        }

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Stream0> stream0;
        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            NativeArray<VertexAttributeDescriptor> descriptors = new NativeArray<VertexAttributeDescriptor>(
                4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            descriptors[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptors[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3);
            descriptors[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4);
            descriptors[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2);
            meshData.SetVertexBufferParams(vertexCount, descriptors);
            descriptors.Dispose();

            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            },
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices);

            stream0 = meshData.GetVertexData<Stream0>();
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, PVertex vertex) => stream0[index] = new Stream0
        {
            position = vertex.position,
            normal = vertex.normal,
            tangent = vertex.tangent,
            texCoord0 = vertex.texCoord0
        };

        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
    }

    public struct MultiStream : IMeshStreams
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TriangleUInt16
        {
            public ushort a, b, c;

            public static implicit operator TriangleUInt16(int3 t) => new TriangleUInt16
            {
                a = (ushort)t.x,
                b = (ushort)t.y,
                c = (ushort)t.z

            };
        }

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> stream0, stream1;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float4> stream2;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float2> stream3;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            NativeArray<VertexAttributeDescriptor> descriptors = new NativeArray<VertexAttributeDescriptor>(
                4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            descriptors[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptors[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1);
            descriptors[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4, stream: 2);
            descriptors[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 3);
            meshData.SetVertexBufferParams(vertexCount, descriptors);
            descriptors.Dispose();

            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            },
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices);

            stream0 = meshData.GetVertexData<float3>();
            stream1 = meshData.GetVertexData<float3>(1);
            stream2 = meshData.GetVertexData<float4>(2);
            stream3 = meshData.GetVertexData<float2>(3);
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, PVertex vertex)
        {
            stream0[index] = vertex.position;
            stream1[index] = vertex.normal;
            stream2[index] = vertex.tangent;
            stream3[index] = vertex.texCoord0;
        }


        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
    }
}



namespace ProceduralMeshes.Generators
{
   
    public struct CubeSphere : IMeshGenerator
    {
        public int VertexCount => 6 * 4 * Resolution * Resolution;

        public int IndexCount => 6 * 6 * Resolution * Resolution;

        public int JobLength => 6 * Resolution;

        public int Resolution { get; set; }
        public Passer passer { get; set; }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        struct Side
        {
            public int id;
            public float3 uvOrigin, uVector, vVector;
            
        }

        static Side GetSide(int id) => id switch {
            0 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * right(),
                vVector = 2f * up(),
            },
            1 => new Side
            {
                id = id,
                uvOrigin = float3(1f, -1f, -1f),
                uVector = 2f * forward(),
                vVector = 2f * up(),
            },
            2 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * forward(),
                vVector = 2f * right(),
            },
            3 => new Side
            {
                id = id,
                uvOrigin = float3(-1f, -1f, 1f),
                uVector = 2f * up(),
                vVector = 2f * right(),
            },
            4 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * up(),
                vVector = 2f * forward(),
            },
            _ => new Side
            {
                id = id,
                uvOrigin = float3(-1f, 1f, -1f),
                uVector = 2f * right(),
                vVector = 2f * forward(),
            },
        };

        static float3 CubeToSphere (float3 p) => p * sqrt(
			1f - ((p * p).yxx + (p * p).zzy) / 2f + (p * p).yxx * (p * p).zzy / 3f
		);

        struct NoiseFilter
        {
            NoiseSettings noiseSettings;

            public NoiseFilter(NoiseSettings settings)
            {
                this.noiseSettings = settings;
            }

            public float Evaluate(float3 point)
            {
                float noiseValue = 0f;
                float frequency = noiseSettings.baseRoughness;
                float amplitude = 1f;

                for (int i = 0; i < noiseSettings.numLayers; i++)
                {
                    float v = snoise(point * frequency + noiseSettings.center);
                    noiseValue += (v + 1) * 0.5f * amplitude;
                    frequency *= noiseSettings.roughness;
                    amplitude *= noiseSettings.persistence;
                }

                noiseValue = max(0f, noiseValue - noiseSettings.minValue);
                return noiseValue * noiseSettings.strength;
            }
        }
      
        static float3 CalculateElevation(float3 point, Passer passer)
        {
            float elevation = 0;
            float firstLayerValue = 0;
            var nf = new NoiseFilter(passer.nl1.noiseSettings);
            
            if (passer.nl1.active)
                firstLayerValue = nf.Evaluate(point);
            if (passer.nl1.active)
                elevation = firstLayerValue;

            nf = new NoiseFilter(passer.nl2.noiseSettings);
            if (passer.nl2.active)
            {
                float mask = passer.nl2.useFirstLayerAsMask ? firstLayerValue : 1;
                elevation += nf.Evaluate(point) * mask;
            }

            nf = new NoiseFilter(passer.nl3.noiseSettings);
            if (passer.nl3.active)
            {
                float mask = passer.nl3.useFirstLayerAsMask ? firstLayerValue : 1;
                elevation += nf.Evaluate(point) * mask;

            }

            nf = new NoiseFilter(passer.nl4.noiseSettings);
            if (passer.nl4.active)
            {
                float mask  = passer.nl3.useFirstLayerAsMask ? firstLayerValue : 1;
                elevation += nf.Evaluate(point) * mask;

            }


            return point * (1 + elevation);
        }


        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            int u = i / 6;
            Side side = GetSide(i - 6 * u);

            int vi = 4 * Resolution * (Resolution * side.id + u);
            int ti = 2 * Resolution * (Resolution * side.id + u);

            float3 uA = side.uvOrigin + side.uVector * u / Resolution;
            float3 uB = side.uvOrigin + side.uVector * (u + 1) / Resolution;
            float3 pA = CubeToSphere(uA), pB = CubeToSphere(uB);
            pA = CalculateElevation(pA, passer);
            pB = CalculateElevation(pB, passer);
  
            PVertex vertex = new PVertex();
            vertex.tangent = float4(normalize(pB - pA), -1f);

            for (int v = 1; v <= Resolution; v++, vi += 4, ti += 2)
            {
                float3 pC = CubeToSphere(uA + side.vVector * v / Resolution);
                pC = CalculateElevation(pC, passer);
                float3 pD = CubeToSphere(uB + side.vVector * v / Resolution);
                pD = CalculateElevation(pD, passer);
               
                vertex.position = pA;
                vertex.normal = normalize(cross(pC - pA, vertex.tangent.xyz));
                vertex.texCoord0 = 0f;
                streams.SetVertex(vi + 0, vertex);

                vertex.position = pB;
                vertex.normal = normalize(cross(pD - pB, vertex.tangent.xyz));
                vertex.texCoord0 = float2(1f, 0f);
                streams.SetVertex(vi + 1, vertex);

                vertex.position = pC;
                vertex.tangent.xyz = normalize(pD - pC);
                vertex.normal = normalize(cross(pC - pA, vertex.tangent.xyz));
                vertex.texCoord0 = float2(0f, 1f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position = pD;
                vertex.texCoord0 = 1f;
                vertex.normal = normalize(cross(pD -pB, vertex.tangent.xyz));
                streams.SetVertex(vi + 3, vertex);
                streams.SetTriangle(ti + 0, vi + int3(0, 2, 1));
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3));

                pA = pC;
                pB = pD;
            }

        }
    }
}