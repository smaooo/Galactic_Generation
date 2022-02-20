using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Mathematics;

using static Unity.Mathematics.math;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomMultiStreamProceduralMesh : MonoBehaviour
{
    private void Start()
    {
        CreateMesh();
    }

    void CreateMesh()
    {
        Bounds bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1f, 1f));
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        Mesh mesh = new Mesh { name = "Mesh", bounds = bounds };
        int vertexAttributeCount = 4;
        int vertexCount = 4;
        int triangleIndexCount = 6;
        NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>
            (vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3, stream: 1);
        vertexAttributes[2] = new VertexAttributeDescriptor(
            VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 2);
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 3);
        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        NativeArray<float3> positions = meshData.GetVertexData<float3>();
        positions[0] = 0f;
        positions[1] = Vector3.right;
        positions[2] = Vector3.up;
        positions[3] = float3(1f, 1f, 0f);

        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = normals[1] = normals[2] = normals[3] = Vector3.back;
        half h0 = half(0f), h1 = half(1f);
        NativeArray<half4> tangents = meshData.GetVertexData<half4>(2);
        tangents[0] = tangents[1] = tangents[2] = tangents[3] = half4(half(1f), half(0f), half(0f), half(-1f));

        NativeArray<half2> texCoords = meshData.GetVertexData<half2>(3);
        texCoords[0] = half(0f);
        texCoords[1] = half2(half(1f), half(0f));
        texCoords[2] = half2(half(0f), half(1f));
        texCoords[3] = half(1f);

        NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
        triangleIndices[0] = 0;
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        triangleIndices[3] = 1;
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds) ;
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
