using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;
using System.Linq;

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
        MeshJob<UVSphere, SingleStream>.ScheduleParallel
    };

    public enum MeshType
    {
        SquareGrid, SharedSquareGrid, SharedTriangleGrid, PointyHexagonGrid, FlatHexagonGrid,
        UVSphere
    }

    [SerializeField]
    MeshType meshType;

    [SerializeField, Range(1, 50)]
    private int resolution = 1;
    Mesh mesh;
    [SerializeField]
    private Slider slider;

    private List<Vector3> vertices, normals;
    private List<Vector4> tangents;
    [SerializeField]
    private Dropdown dropDown;

    [System.Flags]
    public enum GizmoMode { Nothing = 0, Vertices = 1, Normals = 0b10, Tangents = 0b100}
    [SerializeField]
    private GizmoMode gizmoMode;

    private void Awake()
    {
        List<string> options = new List<string>();
        foreach (var m in Enum.GetValues(typeof(MeshType)))
        {
            options.Add(m.ToString());
        }
        dropDown.AddOptions(options);
        dropDown.value = (int)meshType;
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
        if ((int)meshType > 4)
        {
            GetComponent<MeshRenderer>().material.EnableKeyword("_TEXTUREMODE_TILEMAP");
            GetComponent<MeshRenderer>().material.DisableKeyword("_TEXTUREMODE_UVMAP");
        }
        else
        {
            GetComponent<MeshRenderer>().material.EnableKeyword("_TEXTUREMODE_UVMAP");
            GetComponent<MeshRenderer>().material.DisableKeyword("_TEXTUREMODE_TILEMAP");

        }
        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        tangents = new List<Vector4>();
    }

    private void OnDrawGizmos()
    {
        if (gizmoMode == GizmoMode.Nothing || mesh == null)
        {
            return;
        }

        bool drawVertices = (gizmoMode & GizmoMode.Vertices) != 0;
        bool drawNormals = (gizmoMode & GizmoMode.Normals) != 0;
        bool drawTangents = (gizmoMode & GizmoMode.Tangents) != 0;

        if (vertices.Count == 0)
        {
            vertices = new List<Vector3>(mesh.vertices);
        }
        if (drawNormals && normals.Count == 0)
        {
            normals = new List<Vector3>(mesh.normals);
        }
        if (drawTangents && tangents.Count == 0)
        {
            tangents = new List<Vector4>(mesh.tangents);
        }
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 p = transform.TransformPoint(vertices[i]);
            if (drawVertices)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(p, 0.02f);
            }
            if (drawNormals)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(p, transform.TransformPoint(normals[i]) * 0.2f);
            }
            if (drawTangents)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(p, transform.TransformPoint(tangents[i]) * 0.2f);
            }
        }
    }

    private void GenerateMesh()
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        jobs[(int)meshType](mesh, meshData, resolution, default).Complete();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
    }
}
