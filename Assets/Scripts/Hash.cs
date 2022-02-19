using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using float4x4 = Unity.Mathematics.float4x4;
using System;

public class Hash : MonoBehaviour
{
    [Serializable]
    public struct SpaceTRS
    {

        public float3 translation, rotation, scale;

        public float3x4 Matrix
        {
            get
            {
                float4x4 m = float4x4.TRS(translation, quaternion.EulerZXY(radians(rotation)), scale);
                return float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);
            }
        }
    }


    [SerializeField]
    private Material material;

    [SerializeField]
    private int resolution = 16;

    [SerializeField]
    private int seed = 0;

    [SerializeField, Range(-200f, 200f)]
    private float verticalOffset = 1f;
    
    [SerializeField]
    Mesh instanceMesh;

    private int length;
    private uint[] hashes;

    [SerializeField]
    private SpaceTRS domain = new SpaceTRS();
    float invResolution;
    private List<GameObject> cubes = new List<GameObject>();
    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;
    ComputeBuffer hashesBuffer;
    void Start()
    {

        length = resolution * resolution;
        hashesBuffer = new ComputeBuffer(length, 4);
        invResolution = 1f / resolution;
        matrices = new Matrix4x4[length];
        hashes = new uint[length];
        propertyBlock = new MaterialPropertyBlock();
        CreateHash();
        
    }

    private void CreateHash()
    {
        cubes.Clear();
        List<float3> positions = new List<float3>();
        
        for (int i = 0; i < length; i++)
        {

            positions.Add(CreateShape(resolution, invResolution, i, transform.localToWorldMatrix));
           
            float3x4 domainTRS = domain.Matrix;
            float3 p = mul(domainTRS, float4(positions[i], 1f));
            int u = (int)floor(p.x);
            int v = (int)floor(p.y);
            int w = (int)floor(p.z);

            hashes[i] = XXHash.Seed(seed).Eat(u).Eat(v).Eat(w);
        }

        float[] R = new float[hashes.Length - 1];
        float[] G = new float[hashes.Length - 1];
        float[] B = new float[hashes.Length - 1];
        print(R.Length);
     
        Vector3 config = new Vector3(resolution,
            1f / resolution, verticalOffset / resolution);
        
        for (int i = 0; i < hashes.Length; i++)
        {
            var hash = hashes[i];
           

            float4 color = (1.0f / 255.0f) * float4(
                            hash & 255,
                            (hash >> 8) & 255,
                            (hash >> 16) & 255, 1f
                        );

            material.SetVector("_Colors" + i.ToString(), color);
            positions[i] = float3(positions[i].x, config.z * ((1.0f / 255.0f) * (hash >> 24) - 0.5f), positions[i].z);
            float4x4 matrix = float4x4.TRS(positions[i], Quaternion.identity, Vector3.one * config.y);
            matrices[i] = matrix;
          
        }
        
        propertyBlock = new MaterialPropertyBlock();
        hashesBuffer.SetData(hashes);
        propertyBlock.SetBuffer("_Hashes", hashesBuffer);

        //float[] fHashes = new float[hashes.Length - 1];
        //for (int i = 0; i < length - 1; i++)
        //{
        //    fHashes[i] = hashes[i];
        //}
        //material.SetFloatArray("_Hashes", fHashes);

        //propertyBlock.SetFloatArray("_Hashes", fHashes);
        //Destroy(cube);

        //cubes.OrderBy(c => c.transform.position.y).ToList();

        //foreach(var c in cubes)
        //{
        //    StartCoroutine(Move(cubes[0].transform.position, c.transform, cubes.Last().transform.position, config.y));
        //}

    }
    
    private void Update()
    {
        Graphics.DrawMeshInstanced(instanceMesh, 0, material, matrices, length - 1, propertyBlock);

        //Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one), )
        //Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one), length, propertyBlock);
    }
    void OnValidate()
    {
        //if (hashesBuffer != null && enabled)
        //{
        //    OnDisable();
        //    CreateHash();
        //}

        //if (Application.isPlaying)
        //{
        //    OnDisable();
        //    for (int i = 0; i < this.transform.childCount; i++)
        //    {
        //        Destroy(this.transform.GetChild(i).gameObject);
        //    }
        //    hashesBuffer = new ComputeBuffer(length, 4);

        //    length = resolution * resolution;
        //    hashes = new uint[length];
        //    matrices = new Matrix4x4[length];

        //    CreateHash();

        //}
    }
  
    public static float3 CreateShape(float resolution, float invResolution, int id, float4x4 trs)
    {
        float2 uv;
        uv.y = floor(invResolution * id + 0.00001f);
        uv.x = invResolution * (id - resolution * uv.y + 0.5f) - 0.5f;
        uv.y = invResolution * (uv.y + 0.5f) - 0.5f;

        float3x4 positionTRS = float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz);
        float3 position = mul(positionTRS, float4(uv.x, 0f, uv.y, 1f));

        return position;
    }

    private void OnDisable()
    {
        hashesBuffer.Dispose();
        hashesBuffer.Release();
    }
}
