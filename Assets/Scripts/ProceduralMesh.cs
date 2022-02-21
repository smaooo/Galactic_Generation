using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine.Rendering;
using UnityEngine.UI;

//https://catlikecoding.com/unity/tutorials/procedural-meshes/square-grid/

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{

    static MeshJobScheduleDelegate[] jobs =
    {
        MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedTriangleGrid, SingleStream>.ScheduleParallel,
        MeshJob<PointyHexagonGrid, SingleStream>.ScheduleParallel,
        MeshJob<FlatHexagonGrid, SingleStream>.ScheduleParallel,
    };

    public enum MeshType
    {
        SquareGrid, SharedSquareGrid, SharedTriangleGrid, PointyHexagonGrid, FlatHexagonGrid
    }

    [SerializeField]
    MeshType meshType;

    [SerializeField, Range(1, 50)]
    private int resolution = 1;
    Mesh mesh;
    [SerializeField]
    private Slider slider;
    private void Awake()
    {
        slider.onValueChanged.AddListener(ChangeResolution);
        mesh = new Mesh
        {
            name = "Mesh"
        };

        //GenerateMesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void ChangeValues(Dropdown dropDown)
    {
        meshType = (MeshType)dropDown.value;
        enabled = true;
    }

    public void ChangeResolution(float value)
    {
        print(value);
        enabled = true;
        resolution = (int)slider.value;
        enabled = true;
    }
    private void OnValidate()
    {
        enabled = true;
    }
    private void Update()
    {
        enabled = false;
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        jobs[(int)meshType](mesh, meshData, resolution, default).Complete();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
    }
}
