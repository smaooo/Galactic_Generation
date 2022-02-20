using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Mathematics;

using static Unity.Mathematics.math;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MultiStreamProceduralMesh : MonoBehaviour
{


    void CreateMesh()
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        Mesh mesh = new Mesh { name = "Mesh" };
        int vertexAttributeCount = 4;
        int vertexCount = 4;
        int triangleIndexCount = 6;
        NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>
            (vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3, stream: 1);
        vertexAttributes[2] = new VertexAttributeDescriptor(
            VertexAttribute.Tangent, dimension: 4, stream: 2);
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, dimension: 2, stream: 3);
        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        NativeArray<float3> positions = meshData.GetVertexData<float3>();
        positions[0] = 0f;
        positions[1] = Vector3.right;
        positions[2] = Vector3.up;
        positions[3] = float3(1f,1f,0f);

        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = normals[1] = normals[2] = normals[3] = Vector3.back;

        NativeArray<float4> tangents = meshData.GetVertexData<float4>(2);
        tangents[0] = tangents[1] = tangents[2] = tangents[3] = float4(1f, 0f, 0f, -1f);

        NativeArray<float2> texCoords = meshData.GetVertexData<float2>(3);
        texCoords[0] = 0f;
        texCoords[1] = float2(1f, 0f);
        texCoords[2] = float2(0f, 1f);
        texCoords[3] = 1f;

        NativeArray<uint> triangleIndices = meshData.GetIndexData<uint>();
        triangleIndices[0] = 0;
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        triangleIndices[3] = 1;
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
