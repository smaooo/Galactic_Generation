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

        
        public int NL { get { return numLayers == 0 ? 1 : numLayers; } set { numLayers = value; } }
        public float R { get { return roughness == 0 ? 2f : roughness; } set { roughness = value; } }
        public float S { get { return strength == 0 ? 1f : strength; } set { strength = value; } }
        public float P { get { return persistence == 0 ? 0.5f : persistence; } set { persistence = value; } }
        public float BR { get { return baseRoughness == 0 ? 1f : baseRoughness; } set { baseRoughness = value; } }
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
    public struct SquareGrid : IMeshGenerator
    {
        public int VertexCount => 4 * Resolution * Resolution;

        public int IndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution;

        public int Resolution { get; set; }

        public Passer passer { get; set; }
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            int vi = 4 * Resolution * z, ti = 2 * Resolution * z;

            for (int x = 0; x < Resolution; x++, vi += 4, ti += 2)
            {

                float2 xCoordinates = float2(x, x + 1f) / Resolution - 0.5f;
                float2 zCoordinates = float2(z, z + 1f) / Resolution - 0.5f;
                var vertex = new PVertex();
                vertex.normal.y = 1f;
                vertex.tangent.xw = float2(1f, -1f);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.x;
                streams.SetVertex(vi + 0, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = float2(1f, 0f);
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0f, 1f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = 1f;
                streams.SetVertex(vi + 3, vertex);
                streams.SetTriangle(ti + 0, vi +int3(0, 2, 1));
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3));
            }

        }
    }

    public struct SharedSquareGrid : IMeshGenerator
    {
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        public int IndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution + 1;

        public int Resolution { get; set; }

        public Passer passer { get; set; }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            int vi = (Resolution + 1) * z, ti = 2 * Resolution * (z - 1);

            PVertex vertex = new PVertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            vertex.position.x = -0.5f;
            vertex.position.z = (float)z / Resolution - 0.5f;
            vertex.texCoord0.y = (float)z / Resolution;
            streams.SetVertex(vi, vertex);

            vi++;
            for (int x = 1; x <= Resolution; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x / Resolution - 0.5f;
                vertex.texCoord0.x = (float)x / Resolution;
                streams.SetVertex(vi, vertex);

                if (z > 0)
                {
                    streams.SetTriangle(
                        ti + 0, vi + int3(-Resolution - 2, -1, -Resolution - 1));
                    streams.SetTriangle(
                        ti + 1, vi + int3(-Resolution - 1, -1, 0));
                }
            }
        }
    }

    public struct SharedTriangleGrid : IMeshGenerator
    {
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        public int IndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution + 1;

        public int Resolution { get; set; }
        public Passer passer { get; set; }
        public Bounds Bounds => new Bounds(
            Vector3.zero, new Vector3(1f + 0.5f / Resolution, 0f, sqrt(3f) / 2f));

        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            int vi = (Resolution + 1) * z, ti = 2 * Resolution * (z - 1);

            float xOffset = -0.25f;
            float uOffset = 0f;

            int iA = -Resolution - 2, iB = -Resolution - 1, iC = -1, iD = 0;
            int3 tA = int3(iA, iC, iD);
            int3 tB = int3(iA, iD, iB);

            if ((z & 1) == 1)
            {
                xOffset = 0.25f;
                uOffset = 0.5f / (Resolution + 0.5f);
                tA = int3(iA, iC, iB);
                tB = int3(iB, iC, iD);
            }
            xOffset = xOffset / Resolution - 0.5f;

            PVertex vertex = new PVertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            vertex.position.x = xOffset;
            vertex.position.z = ((float)z / Resolution - 0.5f) * sqrt(3f) / 2f;
            vertex.texCoord0.x = uOffset;
            vertex.texCoord0.y = vertex.position.z / (1f + 0.5f / Resolution) + 0.5f;
            streams.SetVertex(vi, vertex);

            vi++;
            for (int x = 1; x <= Resolution; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x / Resolution + xOffset;
                vertex.texCoord0.x = x / (Resolution + 0.5f) + uOffset;
                streams.SetVertex(vi, vertex);

                if (z > 0)
                {
                    streams.SetTriangle(
                        ti + 0,vi + tA);
                    streams.SetTriangle(
                        ti + 1,vi + tB);
                }
            }
        }
    }

    public struct PointyHexagonGrid : IMeshGenerator
    {
        public int VertexCount => 7 * Resolution * Resolution;

        public int IndexCount => 18 * Resolution * Resolution;

        public int JobLength => Resolution;

        public Passer passer { get; set; }
        public int Resolution { get; set; }
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
            (Resolution > 1 ? 0.75f / Resolution : 0.5f) * sqrt(3f),
            0f,
            0.75f + 0.25f / Resolution));

        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            int vi = 7 * Resolution * z, ti = 6 * Resolution * z;
            

            float h = sqrt(3f) / 4f;

            float2 centerOffest = 0f;

            if (Resolution > 1)
            {
                centerOffest.x = (((z & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
                centerOffest.y = -0.375f * (Resolution - 1);
            }
            for (int x = 0; x < Resolution; x++, vi += 7, ti += 6)
            {
                float2 center = (float2(2f * h * x, 0.75f * z) + centerOffest) / Resolution;
                float2 xCoordinate = center.x + float2(-h, h) / Resolution;
                float4 zCoordinate = center.y + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;

                PVertex vertex = new PVertex();
                vertex.position.xz = center;
                vertex.normal.y = 1f;
                vertex.tangent.xw = float2(1f, -1f);
                vertex.texCoord0 = 0.5f;
                streams.SetVertex(vi + 0, vertex);

                vertex.position.z = zCoordinate.x;
                vertex.texCoord0.y = 0f;
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinate.x;
                vertex.position.z = zCoordinate.y;
                vertex.texCoord0 = float2(0.5f - h, 0.25f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.z = zCoordinate.z;
                vertex.texCoord0.y = 0.75f;
                streams.SetVertex(vi + 3, vertex);

                vertex.position.x = center.x;
                vertex.position.z = zCoordinate.w;
                vertex.texCoord0 = float2(0.5f, 1f);
                streams.SetVertex(vi + 4, vertex);

                vertex.position.x = xCoordinate.y;
                vertex.position.z = zCoordinate.z;
                vertex.texCoord0 = float2(0.5f + h, 0.75f);
                streams.SetVertex(vi + 5, vertex);

                vertex.position.z = zCoordinate.y;
                vertex.texCoord0.y = 0.25f;
                streams.SetVertex(vi + 6, vertex);

                streams.SetTriangle(ti + 0, vi + int3(0, 1, 2));
                streams.SetTriangle(ti + 1, vi + int3(0, 2, 3));
                streams.SetTriangle(ti + 2, vi + int3(0, 3, 4));
                streams.SetTriangle(ti + 3, vi + int3(0, 4, 5));
                streams.SetTriangle(ti + 4, vi + int3(0, 5, 6));
                streams.SetTriangle(ti + 5, vi + int3(0, 6, 1));
                
            }

        }
    }

    public struct FlatHexagonGrid : IMeshGenerator
    {
        public int VertexCount => 7 * Resolution * Resolution;

        public int IndexCount => 18 * Resolution * Resolution;

        public int JobLength => Resolution;

        public Passer passer { get; set; }
        public int Resolution { get; set; }
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
            0.75f + 0.25f / Resolution,
            0f,
            (Resolution > 1 ? 0.75f / Resolution : 0.5f) * sqrt(3f)));

        public void Execute<S>(int x, S streams) where S : struct, IMeshStreams
        {
            int vi = 7 * Resolution * x, ti = 6 * Resolution * x;


            float h = sqrt(3f) / 4f;

            float2 centerOffest = 0f;

            if (Resolution > 1)
            {
                centerOffest.x = -0.375f * (Resolution - 1);
                centerOffest.y = (((x & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
            }
            for (int z = 0; z < Resolution; z++, vi += 7, ti += 6)
            {
                float2 center = (float2(0.75f * x, 2f * h * z) + centerOffest) / Resolution;
                float4 xCoordinate = center.x + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;
                float2 zCoordinate = center.y + float2(h, -h) / Resolution;

                PVertex vertex = new PVertex();
                vertex.position.xz = center;
                vertex.normal.y = 1f;
                vertex.tangent.xw = float2(1f, -1f);
                vertex.texCoord0 = 0.5f;
                streams.SetVertex(vi + 0, vertex);

                vertex.position.x = xCoordinate.x;
                vertex.texCoord0.x = 0f;
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinate.y;
                vertex.position.z = zCoordinate.x;
                vertex.texCoord0 = float2(0.25f, 0.5f + h);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinate.z;
                vertex.texCoord0.x = 0.75f;
                streams.SetVertex(vi + 3, vertex);

                vertex.position.x = xCoordinate.w;
                vertex.position.z = center.y;
                vertex.texCoord0 = float2(1f, 0.5f);
                streams.SetVertex(vi + 4, vertex);

                vertex.position.x = xCoordinate.z;
                vertex.position.z = zCoordinate.y;
                vertex.texCoord0 = float2(0.75f, 0.5f - h);
                streams.SetVertex(vi + 5, vertex);

                vertex.position.x = xCoordinate.y;
                vertex.texCoord0.x = 0.25f;
                streams.SetVertex(vi + 6, vertex);

                streams.SetTriangle(ti + 0, vi + int3(0, 1, 2));
                streams.SetTriangle(ti + 1, vi + int3(0, 2, 3));
                streams.SetTriangle(ti + 2, vi + int3(0, 3, 4));
                streams.SetTriangle(ti + 3, vi + int3(0, 4, 5));
                streams.SetTriangle(ti + 4, vi + int3(0, 5, 6));
                streams.SetTriangle(ti + 5, vi + int3(0, 6, 1));

            }

        }
    }

    public struct UVSphere : IMeshGenerator
    {
        public int Resolution { get; set; }
        int ResolutionV => 2 * Resolution;

        int ResolutionU => 4 * Resolution;
        public int VertexCount => (ResolutionU + 1) * (ResolutionV + 1);

        public int IndexCount => 6 * ResolutionU * (ResolutionV - 1);
        public Passer passer { get; set; }
        public int JobLength => ResolutionU + 1;

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        
        public void Execute<S> (int u, S streams) where S : struct, IMeshStreams
        {
            if (u == 0)
            {
                ExecuteSeam(streams);
            }
            else ExecuteRegular(u, streams);
        }

        public void ExecuteRegular<S>(int u, S streams) where S : struct, IMeshStreams
        {
            int vi = (ResolutionV + 1) * u - 2, ti = 2 * (ResolutionV -1 )* (u - 1);

            PVertex vertex = new PVertex();
            vertex.position.y = vertex.normal.y = -1f;
            sincos(2f * PI * (u - 0.5f) / ResolutionU,
                out vertex.tangent.z, out vertex.tangent.x);
            vertex.tangent.w = -1f;
            vertex.texCoord0.x = (u - 0.5f) / ResolutionU;
            streams.SetVertex(vi, vertex);
            vertex.position.y = vertex.normal.y = 1f;
            vertex.texCoord0.y = 1f;
            streams.SetVertex(vi + ResolutionV, vertex);
            vi++;
            float2 circle;
            sincos(2f * PI * u / ResolutionU, out circle.x, out circle.y);
            vertex.tangent.xz = circle.yx;
            circle.y = -circle.y;
            
            vertex.texCoord0.x = (float)u / ResolutionU;

            int shiftLeft = (u == 1 ? 0 : -1) - ResolutionV;

            streams.SetTriangle(
                ti, vi + int3(-1,  shiftLeft, 0));
            ti++;
            for (int v = 1; v < ResolutionV; v++, vi++)
            {
                sincos(PI + PI * v / ResolutionV,
                    out float circleRadius, out vertex.position.y);
                vertex.position.xz = circle * -circleRadius;
                vertex.normal = vertex.position;
                vertex.texCoord0.y = (float)v / ResolutionV;
                streams.SetVertex(vi, vertex);

                if (v > 1)
                {
                    streams.SetTriangle(
                        ti + 0, vi + int3(shiftLeft - 1, shiftLeft, -1));
                    streams.SetTriangle(
                        ti + 1, vi + int3(-1, shiftLeft, 0));
                    ti += 2;
                }
            }

            streams.SetTriangle(
                ti, vi + int3(shiftLeft - 1, 0, -1));
            
        }

        public void ExecuteSeam<S>(S streams) where S : struct, IMeshStreams
        {

            PVertex vertex = new PVertex();
            vertex.tangent.x = 1f;
            vertex.tangent.w = -1f;
            
            
            for (int v = 1; v < ResolutionV; v++)
            {
                sincos(PI + PI * v / ResolutionV,
                    out vertex.position.z, out vertex.position.y);
                vertex.normal = vertex.position;
                vertex.texCoord0.y = (float)v / ResolutionV;
                streams.SetVertex(v - 1, vertex);

                
            }

            
        }
    }

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
        //static float CreateNoise(float3 point, NativeArray<NoiseLayer> noiseLayers)
        //{
            

        //    return 0.0f;
        //}

        static float3 CalculateElevation(float3 point, Passer passer)
        {
            float elevation = 0;
            float firstLayerValue = 0;
            //for (int i = 0; i < 4; i++)
            //{
            //    var nf = new NoiseFilter(pas.noiseSettings);
            //    elevation += nf.Evaluate(point);
            //}
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
            //pA = float3(pA.x, Mathf.PerlinNoise(pA.x, pA.z), pA.z);
            //pB = float3(pB.x, Mathf.PerlinNoise(pB.x, pB.z), pB.z);
            PVertex vertex = new PVertex();
            vertex.tangent = float4(normalize(pB - pA), -1f);

            for (int v = 1; v <= Resolution; v++, vi += 4, ti += 2)
            {
                float3 pC = CubeToSphere(uA + side.vVector * v / Resolution);
                pC = CalculateElevation(pC, passer);
                float3 pD = CubeToSphere(uB + side.vVector * v / Resolution);
                pD = CalculateElevation(pD, passer);
                //pC = float3(pC.x, Mathf.PerlinNoise(pC.x, pC.z), pC.z);
                //pD = float3(pD.x, Mathf.PerlinNoise(pD.x, pD.z), pD.z);

                //vertex.position = pA * (1 + CreateNoise(pA, noiseSettings));
                vertex.position = pA;
                vertex.normal = normalize(cross(pC - pA, vertex.tangent.xyz));
                vertex.texCoord0 = 0f;
                streams.SetVertex(vi + 0, vertex);

                //vertex.position = pB * (1 + CreateNoise(pB, noiseSettings));
                vertex.position = pB;
                vertex.normal = normalize(cross(pD - pB, vertex.tangent.xyz));
                vertex.texCoord0 = float2(1f, 0f);
                streams.SetVertex(vi + 1, vertex);

                //vertex.position = pC * (1 + CreateNoise(pC, noiseSettings));
                vertex.position = pC;
                vertex.tangent.xyz = normalize(pD - pC);
                vertex.normal = normalize(cross(pC - pA, vertex.tangent.xyz));
                vertex.texCoord0 = float2(0f, 1f);
                streams.SetVertex(vi + 2, vertex);

                //vertex.position = pD * (1 + CreateNoise(pD, noiseSettings));
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