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
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections;

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
        MeshJob<UVSphere, SingleStream>.ScheduleParallel,
        MeshJob<CubeSphere, SingleStream>.ScheduleParallel
    };

    public enum MeshType
    {
        SquareGrid, SharedSquareGrid, SharedTriangleGrid, PointyHexagonGrid, FlatHexagonGrid,
        UVSphere, CubeSphere
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

    //[SerializeField]
    //private float strength = 1f;
    //[SerializeField]
    //private float roughness = 1f;
    //[SerializeField]
    //private Vector3 center;
    //[SerializeField]
    //public NoiseSettings noiseSettings = new NoiseSettings();

    [SerializeField]
    private NoiseLayer[] noiseLayers;
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

    //private void ChangeSettings()
    //{
    //    NoiseSettings.center = float3(center.x, center.y, center.z);
    //    NoiseSettings.roughness = roughness;
    //    NoiseSettings.strength = strength;
    //}
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
        //ChangeSettings();
        GenerateMesh();
      
        //if ((int)meshType > 4)
        //{
        //    GetComponent<MeshRenderer>().material.EnableKeyword("_TEXTUREMODE_TILEMAP");
        //    GetComponent<MeshRenderer>().material.DisableKeyword("_TEXTUREMODE_UVMAP");
        //}
        //else
        //{
        //    GetComponent<MeshRenderer>().material.EnableKeyword("_TEXTUREMODE_UVMAP");
        //    GetComponent<MeshRenderer>().material.DisableKeyword("_TEXTUREMODE_TILEMAP");

        //}
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

        //NativeArray<NoiseLayer> pass = new NativeArray<NoiseLayer>(noiseLayers, Allocator.Persistent);
        Passer passer = new Passer
        {
            nl1 = noiseLayers[0].active ? noiseLayers[0] : new NoiseLayer(),
            nl2 = noiseLayers[1].active ? noiseLayers[1] : new NoiseLayer(),
            nl3 = noiseLayers[2].active ? noiseLayers[2] : new NoiseLayer(),
            nl4 = noiseLayers[3].active ? noiseLayers[3] : new NoiseLayer()
        };

        //for (int i = 0; i < noiseLayers.Length; i++)
        //{
        //    noiseLayersNative[i] = noiseLayers[i];
        //}

        jobs[(int)meshType](mesh, meshData, resolution, default, passer).Complete();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        //pass.Dispose();
        //var vertices = GetComponent<MeshFilter>().mesh.vertices;
        //var normals = GetComponent<MeshFilter>().mesh.normals;
        //List<Vector3>[] vertexNormals = new List<Vector3>[vertices.Length];
        //var triangles = GetComponent<MeshFilter>().mesh.triangles;

        //for (int i = 0; i < vertexNormals.Length; i++)
        //{
        //    vertexNormals[i] = new List<Vector3>();
        //}
        //for (int i = 0; i < triangles.Length; i++)
        //{
        //    Vector3 curNormal = Vector3.Cross(
        //        (vertices[triangles[i + 1]] - vertices[triangles[i]]).normalized,
        //        (vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized);

        //    vertexNormals[triangles[i]].Add(curNormal);
        //    vertexNormals[triangles[i + 1]].Add(curNormal);
        //    vertexNormals[triangles[i + 2]].Add(curNormal);
        //}

        //for (int i = 0; i < vertexNormals.Length; i++)
        //{
        //    normals[i] = Vector3.zero;
        //    float numNormals = vertexNormals[i].Count;
        //    for (int j = 0; j < numNormals; j++)
        //    {
        //        normals[i] += vertexNormals[i][j];
        //    }
        //    normals[i] /= numNormals;
        //}
        //GetComponent<MeshFilter>().mesh.normals = normals;

    }
}
