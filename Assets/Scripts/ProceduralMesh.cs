using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    [SerializeField, Range(1, 10)]
    private int resolution = 1;

    private void Awake()
    {
        Mesh mesh = new Mesh
        {
            name = "Mesh"
        };

        GenerateMesh(ref mesh);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void GenerateMesh(ref Mesh mesh)
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        MeshJob<SquareGrid, MultiStream>.ScheduleParallel(
            mesh, meshData, resolution, default).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
    }
}
