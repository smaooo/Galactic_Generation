using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;
using Rand = Unity.Mathematics.Random;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;
using System;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Entities;

namespace GalaxyObject
{

    public enum GType { STAR, PLANET, GASPLANET, BELT, MOON}
    public interface IGObject 
    {
        
        public float Radius { get;}

        public Color GasColor { get; set; }

        public GameObject Object { get; }
        
        public Transform Transform { get; }
        
        public Vector3 Position { get; }

        public float Offset { get; }

   
        public string Name { get; }

        public Mesh[] LODMeshes { get; }
        
        public bool isGas { get; }

        public ParticleSystemRenderer ParticleRenderer { get; }

        public ParticleSystem Particle { get; }
        public Mesh Mesh { get; set; }

        public dynamic Parent { get; set; }

        
        public void Instantiate(bool destroy = false, int lodIndex = 0);
    }




    public class SolarSystem
    {
        Planet[] planets;
        Star star;
        GameObject[] lights;
        Transform transform;

        public struct Relations
        {
            public int id;
            public string type;
            public string name;
            public float radius;
            public Vector3 position;
            public Planet planet;
        }

        public SolarSystem(Transform parent, Material planetMaterial, GameObject gas)
        {
            this.transform = parent; 
            this.star = new Star(gas, parent);
            
            this.planets = new Planet[Random.Range(1, 10)];
            float distance = 0f;

            for (int i = 0; i < this.planets.Length; i++)
            {
                distance += Random.Range(10000f, 10000f);

                this.planets[i] = Random.Range(0f, 1f) <= 0.3f && i > 2 ? new Planet(this, gas, planetMaterial, distance, Random.Range(0.5f, 5f))
                    : new Planet(this, planetMaterial, distance, Random.Range(5f, 40f));

            }



            List<Relations> relations = new List<Relations>();
            for (int i = 0; i < this.planets.Length; i++)
            {
                var p = this.planets[i];
                relations.Add(new Relations
                {
                    id = i,
                    type = "PLANET",
                    name = p.Name,
                    radius = p.Radius,
                    position = p.Position
                });
                for (int j = 0; j < this.planets[i].Moons.Length; j++)
                {
                    var m = p.Moons[j];
                    relations.Add(new Relations
                    {
                        id = j,
                        type = "MOON",
                        name = m.Name,
                        radius = m.Radius,
                        position = m.Position,
                        planet = p
                    });
                }
                if (p.isGas)
                {
                    for (int j = 0; j < this.planets[i].Belt.Length; j++)
                    {
                        var b = p.Belt[j];
                        relations.Add(new Relations
                        {
                            id = j,
                            type = "BELT",
                            name = b.Name,
                            radius = b.Radius,
                            position = b.Position,
                            planet = p
                        });
                    }

                }
            }

            Generator.GenerateLights(ref this.lights, this, relations);

            
        }

        public void CheckLOD()
        {
            this.star.CheckLOD();
            foreach (var p in this.planets)
            {
                p.CheckLOD();
            }
        }

        public float Radius
        {
            get
            { return this.star.ParticleRenderer.bounds.size.x / 2f; }
        }
        public Planet[] Planets
        {
            get { return this.planets; }
        }

        public Star Star
        {
            get { return this.star; }
        }

        public Transform Transform
        {
            get { return this.transform; }
        }

        
    }


    public class Star : IGObject
    {
        GameObject star;
        Color initColor;
        SolarSystem parent;

        public Star(GameObject starPrefab, Transform parent)
        {
            this.star = GameObject.Instantiate(starPrefab, parent);
            //this.star.isStatic = true;

            Generator.SetupGas(this);
        }


        public float Radius { get { return this.star.GetComponent<ParticleSystemRenderer>().bounds.size.x / 2f; } }




        public Vector3 Position { get { return this.star.transform.position; } }

        public float Offset { get; }


        public string Name { get; }

        public Mesh[] LODMeshes { get; }

        public bool isGas {get { return true; } }


        public Mesh Mesh { get; set; }
        public GameObject Object
        {
            get { return this.star; }
        }

        public Color GasColor
        {
            get { return this.initColor; }
            set { this.initColor = value; }
        }
        public Transform Transform
        {
            get { return this.star.transform; }
        }

        public ParticleSystemRenderer ParticleRenderer
        {
            get { return this.star.GetComponent<ParticleSystemRenderer>(); }
        }

        public ParticleSystem Particle
        {
            get { return this.star.GetComponent<ParticleSystem>(); }
        }

        public dynamic Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }
        public void CheckLOD() => Generator.CheckLOD(this);
        
        public void Instantiate (bool destroy, int index)
        {

        }
    }


    public class Planet : IGObject
    {
        GameObject planet;
        Moon[] moons;
        Material material;
        float radius;
        float offset;
        SolarSystem parent;
        Mesh[] lodMeshes;
        Belt[] belt;
        bool gasObject;
        Color initColor;
        string name;

        public Planet(SolarSystem parent, Material material, float offset, float radius)
        {
            this.name = "Planet" + GetHashCode();
            this.planet = new GameObject(this.name);
            this.material = material;
            this.radius = radius;
            this.offset = offset;
            this.parent = parent;
            this.gasObject = false;

            this.planet.AddComponent<MeshRenderer>().material = material;
            this.planet.AddComponent<MeshFilter>();

            this.planet.transform.SetParent(this.parent.Transform);

            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);
            Generator.GenerateMesh(this.planet.transform.position, ref lodMeshes, this.planet.name);
            this.planet.GetComponent<MeshFilter>().mesh = this.lodMeshes[0];
            this.planet.transform.RotateAround(this.parent.Star.Position, this.parent.Star.Transform.up, Random.Range(0f,360f));
            this.planet.transform.RotateAround(this.parent.Star.Position, this.parent.Star.Transform.right, Random.Range(0f,360f));
            this.planet.transform.RotateAround(this.parent.Star.Position, this.parent.Star.Transform.forward, Random.Range(0f,360f));

            int numberOfMoons = radius > 1 ? (int)(Random.Range(5, 10)) : (int)(Random.Range(0, 5) * radius);

            this.moons = new Moon[numberOfMoons];
            GenerateMoons();
            
        }

        public Planet(SolarSystem parent, GameObject gas, Material material, float offset, float radius)
        {
            this.parent = parent;
            this.name = "GasPlanet" + GetHashCode();
            this.planet = GameObject.Instantiate(gas, this.parent.Transform);
            this.planet.name = this.name;
            this.material = material;

            this.gasObject = true;
            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);

            this.planet.transform.RotateAround(this.parent.Star.Position, this.parent.Star.Transform.up, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(this.parent.Star.Position, this.parent.Star.Transform.right, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(this.parent.Star.Position, this.parent.Star.Transform.forward, Random.Range(0f, 360f));
            this.radius = this.planet.GetComponent<ParticleSystemRenderer>().bounds.size.x / 2;
            int numberOfMoons = (int)(Random.Range(5, 10));

            this.moons = new Moon[numberOfMoons];

            Generator.SetupGas(this);

            GenerateMoons();

            GenerateBelt();

        }



        public void Instantiate(bool destroy = false, int lodIndex = 0)
        {
            
        }

        public void CheckLOD()
        {

            Generator.CheckLOD(this);
            if (this.gasObject)
                foreach (var b in this.belt)
                {
                    b.CheckLOD();
                }
                
            

            foreach(var m in this.moons)
            {
                m.CheckLOD();
            }
            

        }

        public Color GasColor
        {
            get { return this.initColor; }
            set { this.initColor = value; }
        }

        public GameObject Object
        {
            get { return this.planet; }
        }

        public Moon[] Moons
        {
            get { return this.moons; }
        }

        public Transform Transform
        {
            get { return this.planet.transform; }
        }

        public Vector3 Position
        {
            get { return this.planet.transform.position; }
        }
        public ParticleSystem Particle
        {
            get
            {
                if (this.gasObject)
                {
                    return this.planet.GetComponent<ParticleSystem>();
                }
                else return null;
            }
        }
        public float Offset
        {
            get { return this.offset; }
        }

        public bool isGas
        {
            get { return this.gasObject; }
        }

        public ParticleSystemRenderer ParticleRenderer
        {
            get
            {
                if (this.gasObject)
                {
                    return this.planet.GetComponent<ParticleSystemRenderer>();
                }
                else
                {
                    return null;
                }
            }
        }
        public float Radius
        {
            get
            {
                float r;
                if (this.planet.TryGetComponent<ParticleSystemRenderer>(out ParticleSystemRenderer pR))
                {
                    r = pR.bounds.size.x / 2f;
                }
                else
                {
                    r = this.planet.GetComponent<MeshRenderer>().bounds.size.x / 2f;

                }
                return r;
            }
        }

        public Mesh Mesh
        {
            get
            {
                return this.planet.GetComponent<MeshFilter>().mesh;
            }
            set
            {
                this.planet.GetComponent<MeshFilter>().mesh = value;
            }
        }

        public Mesh[] LODMeshes
        {
            get { return this.lodMeshes; }
            set { this.lodMeshes = value; }
        }
        public string Name
        {
            get { return this.planet.name; }
        }

        public Belt[] Belt
        {
            get { return this.belt; }
        }

        public dynamic Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        private void GenerateMoons()
        {
            for (int i = 0; i < moons.Length; i++)
            {
                float mOffset = Random.Range(0.1f, 1f);
                float mRadius = this.radius / Random.Range(50f, 100f);
                
                moons[i] = new Moon(this, this.material, mOffset, mRadius);
            }
            

        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        public struct BeltGenerator : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<float3> positions;
            [WriteOnly]
            public NativeArray<float> scales;
            [ReadOnly]
            public float innerRadius;
            [ReadOnly]
            public float outerRadius;
            [ReadOnly]
            public Matrix4x4 matrix;
            [ReadOnly]
            public float3 position;

            public Rand rand;

            public void Execute(int i)
            {
                float3 pos;
                float3 tmp;
                
                float x;
                float y;
                float z;
                do
                {
                    x = rand.NextFloat(-outerRadius, outerRadius);
                    y = rand.NextFloat(0f, innerRadius / 50f);
                    z = rand.NextFloat(-outerRadius, outerRadius);
                    pos = matrix.MultiplyPoint3x4(float3(x, y, z));
                    
                    tmp = matrix.MultiplyPoint3x4(float3(x, 0f, z));
                        
                    
                }
                while (Vector3.Distance(position, tmp) < innerRadius
                    || Vector3.Distance(position, tmp) > outerRadius);

                scales[i] = rand.NextFloat(0.1f, 0.3f);
                pos = (pos - position) * remap(0f, 1f, 0.8f, 1.2f, snoise(pos));
                positions[i] = matrix.MultiplyPoint3x4(pos);
            }
        }

        private void GenerateBelt()
        {
            int count = Random.Range(1000, 2000);
            float innerRingRadius = this.radius * Mathf.Log10(count) / Random.Range(2f, 2.5f);
            float outerRingRadius = this.radius * Mathf.Log10(count) / Random.Range(1f, 2f);

            NativeArray<float3> results = new NativeArray<float3>(count, Allocator.Persistent);
            NativeArray<float> scaleResults = new NativeArray<float>(count, Allocator.Persistent);
            var job = new BeltGenerator
            {
                positions = results,
                scales = scaleResults,
                innerRadius = innerRingRadius,
                outerRadius = outerRingRadius,
                matrix = this.planet.transform.localToWorldMatrix,
                position = this.planet.transform.position,
                rand = new Rand((uint)Random.Range(1, 100000))
            };
            job.Schedule(count, 4, default).Complete();
            this.belt = new Belt[count];
            
            for (int i = 0; i < results.Length; i++)
            {
                belt[i] = new Belt(this, results[i], scaleResults[i], this.material);

                
            }


            results.Dispose();
            scaleResults.Dispose();

        }


    }

    public class Belt : IGObject
    {
        GameObject beltObject;
        Mesh[] lodMeshes;
        string name;
        Material material;
        Planet parent;

        public Belt(Planet parent, Vector3 position, float scale, Material material)
        {
            this.parent = parent;
            this.name = this.parent.Name + "_BeltObj" + GetHashCode();
            this.beltObject = new GameObject(this.name);
            this.beltObject.AddComponent<MeshRenderer>().material = material;
            this.beltObject.AddComponent<MeshFilter>();
            this.material = material;
            //this.entity = entityManager.Instantiate(entity);
            //entityManager.AddComponentData(this.entity, new Translation
            //{
            //    Value = position
            //});
            //entityManager.AddComponentData(this.entity, new Scale
            //{
            //    Value = scale
            //});

            //this.entityManager = entityManager;
            //Generator.GenerateMesh(position, ref lodMeshes, name);
            //entityManager.AddSharedComponentData(this.entity, new RenderMesh { 
            //    mesh = this.lodMeshes[0],
            //    material = material});

            this.beltObject.transform.position = position;
            this.beltObject.transform.SetParent(this.parent.Transform);
            this.beltObject.transform.localScale = Vector3.one * scale;


        }

        public Mesh Mesh
        {
            get
            {
                return this.beltObject.GetComponent<MeshFilter>().mesh;
            }
            set
            {
                this.beltObject.GetComponent<MeshFilter>().mesh = value;
            }
        }


        public GameObject Object
        {
            get { return this.beltObject; }
        }

        public bool isGas { get { return false; } }
        public Transform Transform
        {
            get { return this.beltObject.transform; }
        }
        
        public Vector3 Position
        {
            get { return this.Transform.position; }
        }
        public float Radius
        {
            get { return this.beltObject.GetComponent<MeshRenderer>().bounds.size.x / 2f; }
        }

        public ParticleSystemRenderer ParticleRenderer { get; }
        public ParticleSystem Particle { get; }
        public string Name
        {
            get { return this.name; }
        }

        public Color GasColor { get; set; }

        public dynamic Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        public float Offset { get; }



        public Mesh[] LODMeshes
        {
            get { return null; }
        }
        public void CheckLOD() => Generator.CheckLOD(this);

        public void Instantiate (bool destroy, int index)
        {

        }
    }


    public class Moon : IGObject
    {

        GameObject moon;
        Planet parent;
        Mesh[] lodMeshes;
        float offset;
        string name;
        
        public Moon(Planet parent, Material material, float offset, float radius)
        {
            this.parent = parent;
            this.name = this.parent.Name + "_Moon" + GetHashCode();
            this.moon = new GameObject(this.name);
            this.offset = offset;

            moon.AddComponent<MeshRenderer>().material = material;
            moon.AddComponent<MeshFilter>();
            moon.transform.SetParent(this.parent.Transform);


            this.moon.transform.localScale = Vector3.one * radius;

            Generator.GenerateMesh(this.moon.transform.position, ref lodMeshes, this.moon.name);
            this.moon.GetComponent<MeshFilter>().mesh = this.lodMeshes[0];
            this.moon.transform.position = new Vector3(offset, 0f, offset);

            

            this.moon.transform.RotateAround(this.parent.Position, this.parent.Transform.up, Random.Range(0f, 360f));
            this.moon.transform.RotateAround(this.parent.Position, this.parent.Transform.right, Random.Range(0f, 360f));
            this.moon.transform.RotateAround(this.parent.Position, this.parent.Transform.forward, Random.Range(0f, 360f));
        }

        public GameObject Object
        {
            get { return this.moon; }
        }
        public ParticleSystem Particle { get; }
        public Transform Transform
        {
            get { return this.moon.transform; }
        }

        public float Radius
        {
            get { return  this.moon.GetComponent<MeshRenderer>().bounds.size.x / 2f; }
        }

        public ParticleSystemRenderer ParticleRenderer { get; }
        public bool isGas { get { return false; } }

        public string Name
        {
            get { return this.moon.name; }
        }


        public Color GasColor { get; set; }

        public dynamic Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        public Vector3 Position
        {
            get { return this.moon.transform.position; }
        }

        public float Offset
        {
            get { return this.offset; }
        }

        public Mesh Mesh
        {
            get
            {
                return this.moon.GetComponent<MeshFilter>().mesh;
            }
            set
            {
                this.moon.GetComponent<MeshFilter>().mesh = value;
            }
        }


        public Mesh[] LODMeshes 
        {
            get { return this.lodMeshes; }
        }
        public void CheckLOD() => Generator.CheckLOD(this);

        public void Instantiate(bool destroy, int index)
        {

        }
    }



    public static class Generator
    {
        static MeshJobScheduleDelegate meshJob = MeshJob<CubeSphere, SingleStream>.ScheduleParallel;


        
        public static void GenerateMesh(Vector3 position, ref Mesh[] lodMeshes, string name)
        {
            NoiseLayer[] noiseLayers = new NoiseLayer[4];

            noiseLayers[0] = new NoiseLayer
            {
                active = true,
                useFirstLayerAsMask = false,
                noiseSettings = new NoiseSettings
                {
                    center = float3(
                            snoise(position) * Random.Range(-100f, 100f),
                            snoise(position) * Random.Range(-100f, 100f),
                            snoise(position) * Random.Range(-100f, 100f)),
                    numLayers = Random.Range(1, 4),
                    roughness = Random.Range(0f, 1f),
                    strength = Random.Range(0f, 0.1f),
                    persistence = Random.Range(0f, 1.2f),
                    baseRoughness = Random.Range(0f, 2f),
                    minValue = Random.Range(0f, 2.5f)


                }
            };

            noiseLayers[1] = new NoiseLayer
            {
                active = true,
                useFirstLayerAsMask = Random.Range(0f, 1f) > 0.3f ? true : false,
                noiseSettings = new NoiseSettings
                {
                    center = float3(
                            snoise(position) * Random.Range(-100f, 100f),
                            snoise(position) * Random.Range(-100f, 100f),
                            snoise(position) * Random.Range(-100f, 100f)),
                    numLayers = Random.Range(1, 4),
                    roughness = Random.Range(0f, 50f),
                    strength = noiseLayers[1].useFirstLayerAsMask ? Random.Range(-1f, 1f) : Random.Range(-0.1f, 0.1f),
                    persistence = noiseLayers[1].useFirstLayerAsMask ? Random.Range(0f, 10f) : Random.Range(-5f, 1f),
                    baseRoughness = noiseLayers[1].useFirstLayerAsMask ? Random.Range(0f, 10f) : Random.Range(-1f, 1f),
                    minValue = noiseLayers[1].useFirstLayerAsMask ? Random.Range(-5f, 0f) : Random.Range(0f, 1f)

                }
            };

            noiseLayers[2] = new NoiseLayer
            {
                active = true,
                useFirstLayerAsMask = false,
                noiseSettings = new NoiseSettings
                {
                    center = float3(
                            snoise(position) * Random.Range(-100f, 100f),
                            snoise(position) * Random.Range(-100f, 100f),
                            snoise(position) * Random.Range(-100f, 100f)),
                    numLayers = Random.Range(1, 4),
                    roughness = Random.Range(0f, 5f),
                    strength = Random.Range(-0.5f, 0.5f),
                    persistence = Random.Range(-0.3f, 0.3f),
                    baseRoughness = Random.Range(0f, 2f),
                    minValue = Random.Range(0f, 1f)
                }
            };

            noiseLayers[3] = new NoiseLayer
            {
                active = true,
                useFirstLayerAsMask = Random.Range(0f, 1f) > 0.3f ? true : false,
                noiseSettings = new NoiseSettings
                {
                    numLayers = Random.Range(1, 4),
                    roughness = Random.Range(0f, 50f),
                    strength = Random.Range(-0.5f, 1f),
                    persistence = Random.Range(0f, 0.3f),
                    baseRoughness = Random.Range(-5f, 5f),
                    minValue = Random.Range(-2f, 1f)

                }
            };


            Passer passer = new Passer
            {
                nl1 = noiseLayers[0].active ? noiseLayers[0] : new NoiseLayer(),
                nl2 = noiseLayers[1].active ? noiseLayers[1] : new NoiseLayer(),
                nl3 = noiseLayers[2].active ? noiseLayers[2] : new NoiseLayer(),
                nl4 = noiseLayers[3].active ? noiseLayers[3] : new NoiseLayer()
            };

            int resolution = 2;
            lodMeshes = new Mesh[5];

            for (int i = 0; i < lodMeshes.Length; i++)
            {
                Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
                Mesh.MeshData meshData = meshDataArray[0];
                Mesh m = new Mesh { name = name + "_LOD" + i };
                meshJob(m, meshData, resolution, default, passer).Complete();
                lodMeshes[i] = m;
                resolution *= 2;
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, lodMeshes[i]);
            }


            //transform.GetComponent<MeshFilter>().mesh = lodMeshes[0];

        }

        public static void GenerateLights(ref GameObject[] lights, SolarSystem ss, List<SolarSystem.Relations> relations)
        {
            float intensity;
            float range;
            float offset;

            lights = new GameObject[relations.Count];
            
            for (int i = 0; i < relations.Count; i++)
            {
                var r = relations[i];

                if (r.planet != null)
                {
                    offset = 0.996f;
                    range = 0.008f;
                    intensity = 10000f;
                }
                
                else
                {
                    offset = 0.99f;
                    range = 0.02f;
                    intensity = 20000f;
                }

                lights[i] = new GameObject("LightTo_" + r.name);
                Vector3 distance = r.position - ss.Star.Position;
                lights[i].transform.SetParent(ss.Star.Transform);

                lights[i].transform.position = distance * offset;
                var l = lights[i].AddComponent<Light>();
                l.type = LightType.Spot;
                lights[i].transform.rotation = Quaternion.LookRotation(distance);
                l.range = distance.magnitude * range;
                l.intensity = intensity;
                l.innerSpotAngle = (r.radius) * Mathf.Deg2Rad * 180f;

                
                   
            }
        }
        
        
        public static void CheckLOD<T> (T obj) where T : IGObject
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 objToCamera = obj.Position - Camera.main.transform.position;
            float dot = Vector3.Dot(camForward, objToCamera);
            float dotOverMag = dot / (camForward.magnitude * objToCamera.magnitude);

            float visible = Mathf.Cos(Mathf.Deg2Rad * 60f);
            float distance = Vector3.Distance(Camera.main.transform.position, obj.Position);

            if (obj.LODMeshes == null && obj.isGas)
            {

                if (dotOverMag > visible)
                {
                    if (distance > 100 * obj.Radius)
                        obj.ParticleRenderer.enabled = false;

                    else
                        obj.ParticleRenderer.enabled = true;
                }
                else
                    obj.ParticleRenderer.enabled = false;

                return;
            }
            if (dotOverMag > visible)
            {


                if (distance > 1000f)
                {
                    obj.Mesh = null;
                    if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.enabled = false;
                    }
                    if (obj.Object != null) obj.Object.SetActive(false);
                }
                else if (distance > 600f && distance <= 1000f)
                {
                    if (obj.Object != null) obj.Object.SetActive(true);
                    if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.enabled = true;
                    }
                    obj.Mesh = obj.LODMeshes[0];
                }
                else if (distance > 400f && distance <= 600f)
                {
                    if (obj.Object != null) obj.Object.SetActive(true);
                    if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.enabled = true;
                    }
                    obj.Mesh = obj.LODMeshes[1];
                }
                else if (distance > 200f && distance <= 400f)
                {
                    if (obj.Object != null) obj.Object.SetActive(true);
                    if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.enabled = true;
                    }
                    obj.Mesh = obj.LODMeshes[2];
                }
                else if (distance > 100f && distance <= 200f)
                {
                    if (obj.Object != null) obj.Object.SetActive(true);
                    if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.enabled = true;
                    }
                    obj.Mesh = obj.LODMeshes[3];
                }
                else if (distance <= 100f)
                {
                    if (obj.Object != null) obj.Object.SetActive(true);
                    if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.enabled = true;
                    }
                    obj.Mesh= obj.LODMeshes[4];
                }

            }
            else
            {
                obj.Mesh = null;
                if (obj.Object.TryGetComponent<MeshRenderer>(out var mr))
                {
                    mr.enabled = false;
                }
                if (obj.Object != null) obj.Object.SetActive(false);
            }

        }

        public static void SetupGas<T>(T obj) where T : IGObject
        {
            var main = obj.Particle.main;

            float colorMin;
            float colorMax;

            float randomSelect = Random.Range(0f, 1f);
            if (randomSelect <= 0.2f)
            {
                colorMin = 0f;
                colorMax = 0.05f;
            }
            else if (randomSelect > 0.2f && randomSelect <= 0.4f)
            {
                colorMin = 0.05f;
                colorMax = 0.2f;
            }
            else if (randomSelect > 0.4f && randomSelect <= 0.6f)
            {
                colorMin = 0.2f;
                colorMax = 0.4f;
            }
            else if (randomSelect > 0.6f && randomSelect <= 0.8f)
            {
                colorMin = 0.4f;
                colorMax = 0.65f;
            }
            else
            {
                colorMin = 0.95f;
                colorMax = 1f;
            }
            obj.GasColor = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);
            main.startColor = obj.GasColor;

            obj.Particle.GetComponent<ParticleSystemRenderer>().material.SetVector("_EmissionColor",
                new Vector4(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b,
                4f));

            obj.Particle.GetComponent<ParticleSystemRenderer>().material.SetColor("_BaseColor", obj.GasColor);

            //Gradient colorGrad = new Gradient();
            //var colorOverL = particle.colorOverLifetime;
            //Color color1 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f,1f), 1f);
            //Color color2 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);

            //colorGrad.SetKeys(new GradientColorKey[] {
            //new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, 0.5f)},
            //    new GradientAlphaKey[] {
            //    new GradientAlphaKey(1f,0f), new GradientAlphaKey(Random.Range(0f, 0.2f),Random.Range(0.4f, 0.8f))});
            //colorOverL.color = colorGrad;

            ////var colorBySpeed = particle.colorBySpeed;

            //color1 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);
            //color2 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);

            //colorGrad.SetKeys(new GradientColorKey[] {
            //new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, Random.Range(0.4f, 0.8f))},
            //    new GradientAlphaKey[] {
            //    new GradientAlphaKey(1f,0f), new GradientAlphaKey(Random.Range(0f, 0.2f),Random.Range(0.4f, 0.8f))});

            //colorBySpeed.color = colorGrad;
        }
    }

    
}
