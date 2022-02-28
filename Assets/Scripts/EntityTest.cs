using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using Unity.Jobs;
using Unity.Collections;

public class EntityTest : MonoBehaviour
{
    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Mesh mesh2;
    [SerializeField]
    private Material material;
    [SerializeField]
    private GameObject starParticle;


    private EntityManager entityManager;
    private NativeArray<Entity> entities;
    public int num;
    void Start()
    {
        entities = new NativeArray<Entity>(num, Allocator.Persistent);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var archeType = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(Rotation),
                typeof(MeshRenderer),
                typeof(RenderBounds),
                typeof(LocalToWorld)
                );
        var myEntity = entityManager.CreateEntity(archeType);
        entityManager.Instantiate(myEntity, entities);
        for (int i = 0; i < num; i++)
        {
            var e = entities[i];
            entityManager.AddComponentData(e, new Translation
            {
                Value = new float3(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f))
            });

            entityManager.AddSharedComponentData(e, new RenderMesh
            {
                mesh = mesh,
                material = material
            });

        }

        var m = entityManager.GetSharedComponentData<RenderMesh>(entities[0]);
        m.mesh = mesh2;   
    }


    private void OnDisable()
    {
        entities.Dispose();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            print("Pressed");
            for (int i = 0; i < entities.Length; i++)
            {
                entityManager.AddSharedComponentData(entities[i], new RenderMesh { mesh = mesh2, material = material });
            }

        }

        if (Input.GetKey(KeyCode.Return))
        {
            for (int i = 0; i < entities.Length; i++)
            {
                entityManager.AddSharedComponentData(entities[i], new RenderMesh { mesh = mesh, material = material });
            }

        }
    }
}
