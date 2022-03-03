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
using System.Linq;

namespace GalaxyObject
{
    public class GData
    {
        public string name;
        public Vector3 position;
        public Vector3 scale;
        public Material material;
        public GLight light;
        public GParticle particle;
        public GData parent;
        
        public void Instantiate<T>(T obj) where T : IGObject
        {
            GameObject g;
            if (obj.isGas)
            {
                g = this.particle.Instantiate();
            }
            else
            {
                g = new GameObject();
                g.AddComponent<MeshFilter>().mesh = obj.Mesh;
                g.AddComponent<MeshRenderer>().material = obj.ObjectData.material;
                g.AddComponent<CapsuleCollider>();
                Debug.Log(this.light.name);
                this.light.Instantiate(obj);
            }

            g.name = this.name;
            g.transform.position = this.position;
            g.transform.localScale = this.scale;

            obj.Object = g;
        }


    }

    public struct GLight
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public float intensity;
        public float range;
        public float innerSpotAngle;
        public float outerSpotAngle;

        public void Instantiate<T>(T obj) where T : IGObject
        {
            var gl = new GameObject();
            gl.name = this.name;
            gl.transform.position = this.position;
            gl.transform.rotation = this.rotation;
            var l = gl.AddComponent<Light>();
            l.type = LightType.Spot;
            l.intensity = this.intensity;
            l.range = this.range;
            l.innerSpotAngle = this.innerSpotAngle;
            //l.spotAngle = this.outerSpotAngle;

            obj.Light = gl;

        }

    }

    public struct GParticle
    {
        public string name;
        public GameObject prefab;
        public Color initColor;
       
        public GameObject Instantiate()
        {
            var obj = GameObject.Instantiate(this.prefab);
            
            var main = obj.GetComponent<ParticleSystem>().main;
            main.startColor = this.initColor;

            obj.GetComponent<ParticleSystemRenderer>().material
                .SetVector("_EmissionColor", new Vector4(
                this.initColor.r, this.initColor.g, this.initColor.b, 4f));
            obj.GetComponent<ParticleSystemRenderer>().material.
                SetColor("_BaseColor", this.initColor);

            return obj;
        }
    }

    public interface IGObject 
    {
        
        public float Radius { get;}


        public GameObject Object { get; set; }
        
        public Transform Transform { get; }
        
        public Vector3 Position { get; }

        public float Offset { get; }

   
        
        public Mesh[] LODMeshes { get; }
        
        public bool isGas { get; }

        public ParticleSystemRenderer ParticleRenderer { get; }

        public ParticleSystem Particle { get; }
        public Mesh Mesh { get; set; }

        public GData ObjectData { get; set; }

        public GData ParentData { get; set; }


        public GameObject Light { get; set; }

        public void Destroy();
    }



    
    public class SolarSystem: IGObject
    {
        Planet[] planets;
        Star star;
        Transform transform;
        GData data;
        bool destroyed;

        public SolarSystem(Transform parent, Material planetMaterial, GameObject gas)
        {
            this.transform = parent;

            this.destroyed = false;
            this.data = new GData()
            {
                name = "System" + GetHashCode(),
                position = this.transform.position,
                scale = this.transform.lossyScale
            };
            
            this.star = new Star(this, gas);
            this.planets = new Planet[Random.Range(1, 10)];
            float distance = 0f;

            for (int i = 0; i < this.planets.Length; i++)
            {
                distance += Random.Range(100, 1000f);

                this.planets[i] = Random.Range(0f, 1f) <= 0.3f && i > 2
                    ? new Planet(this, gas, planetMaterial, distance, Random.Range(0.5f, 5f))
                    : new Planet(this, planetMaterial, distance, Random.Range(5f, 40f));
            }
        }

        public void CheckLOD()
        {
            if (!this.destroyed)
            {
                this.star.CheckLOD();
                foreach (var p in this.planets)
                {
                    p.CheckLOD();
                }

            }
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

        public float Radius { get; }


        public GameObject Object { get; set; }

        public Vector3 Position { get; }

        public float Offset { get; }

        public Mesh[] LODMeshes { get; }

        public bool isGas { get; }

        public ParticleSystemRenderer ParticleRenderer { get; }

        public ParticleSystem Particle { get; }
        public Mesh Mesh { get; set; }

        public GData ObjectData 
        {   get { return this.data; }
            set { this.data = value; }
        }

        public GData ParentData { get; set; }

        public GameObject Light { get; set; }

        public void Destroy()
        {
            this.Star.Destroy();
            foreach(var p in this.Planets)
            {
                p.Destroy();
            }
        }
    }


    public class Star : IGObject
    {
        GameObject star;
        SolarSystem parent;
        GData data;
        GData parentData;
        
        public Star(SolarSystem sParent, GameObject starPrefab)
        {
            this.parent = sParent;
            this.star = GameObject.Instantiate(starPrefab, this.parent.Transform);
            this.star.transform.SetParent(this.parent.Transform);



            this.parentData = this.parent.ObjectData;

            this.data = new GData
            {
                name = sParent.ObjectData.name + "_Star" + GetHashCode(),
                position = this.star.transform.position,
                scale = this.star.transform.lossyScale,
                parent = this.parentData
                
            };

            Generator.SetupGas(this);

            data.particle.prefab = starPrefab;
            GameObject.Destroy(this.star);

            this.ObjectData.Instantiate(this);
        }


        public float Radius { get { return this.star.GetComponent<ParticleSystemRenderer>().bounds.size.x / 2f; } }




        public Vector3 Position { get { return this.star.transform.position; } }

        public float Offset { get; }

        public Mesh[] LODMeshes { get; }

        public bool isGas {get { return true; } }


        public Mesh Mesh { get; set; }
        public GameObject Object
        {
            get { return this.star; }
            set { this.star = value; }
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


        //public void CheckLOD() => Generator.CheckLOD(this);
        public void CheckLOD() { }


        public GData ObjectData
        {
            get { return this.data; }
            set { this.data = value; }
        }

        public GData ParentData 
        {
            get { return this.parentData; }
            set { this.parentData = value; }

        }

        public GameObject Light { get; set; }

        public void Destroy()
        {
            if (this.Object != null)
            {
                GameObject.Destroy(this.Object);
            }
            
        }

    }


    public class Planet : IGObject
    {
        GameObject planet;
        Moon[] moons;
        float radius;
        float offset;
        SolarSystem parent;
        Mesh[] lodMeshes;
        Belt[] belt;
        bool gasObject;
        GameObject light;
        GData data;
        GData parentData;
        Mesh mesh;

        public Planet(SolarSystem parent, Material material, float offset, float radius)
        {
            string name = parent.ObjectData.name + "_Planet" + GetHashCode();
            this.planet = new GameObject(name);
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
            this.mesh = lodMeshes[0];
            this.planet.transform.RotateAround(this.parent.Star.Position,
                this.parent.Star.Transform.up, Random.Range(0f,360f));
            this.planet.transform.RotateAround(this.parent.Star.Position,
                this.parent.Star.Transform.right, Random.Range(0f,360f));
            this.planet.transform.RotateAround(this.parent.Star.Position,
                this.parent.Star.Transform.forward, Random.Range(0f,360f));

            int numberOfMoons = radius > 1 
                ? (int)(Random.Range(5, 10)) 
                : (int)(Random.Range(0, 5) * radius);

            this.moons = new Moon[numberOfMoons];
            this.parentData = this.parent.ObjectData;

            this.data = new GData
            {
                name = name,
                position = this.planet.transform.position,
                scale = this.planet.transform.lossyScale,
                material = material,
                parent = this.parentData
            };

            Generator.CreateLight(this);
            GenerateMoons();
            GameObject.Destroy(this.planet);
            
        }

        public Planet(SolarSystem parent, GameObject gas, Material material, float offset, float radius)
        {
            this.parent = parent;
            string name = parent.ObjectData.name + "_GasPlanet" + GetHashCode();
            this.planet = GameObject.Instantiate(gas, this.parent.Transform);
            

            this.gasObject = true;
            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);

            this.planet.transform.RotateAround(this.parent.Star.Position,
                this.parent.Star.Transform.up, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(this.parent.Star.Position,
                this.parent.Star.Transform.right, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(this.parent.Star.Position,
                this.parent.Star.Transform.forward, Random.Range(0f, 360f));
            this.radius = this.planet.GetComponent<ParticleSystemRenderer>().bounds.size.x / 2;
            int numberOfMoons = (int)(Random.Range(5, 10));
            this.parentData = parent.ObjectData;
            this.moons = new Moon[numberOfMoons];
            this.data = new GData
            {
                name = name,
                position = this.planet.transform.position,
                scale = this.planet.transform.lossyScale,
                material = material,
                parent = this.parentData
            };

            Generator.SetupGas(this);

            this.data.particle.prefab = gas;
            GenerateMoons();

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (Random.Range(0, 4) == 1)
                {
                    this.ObjectData.name += "WB";
                    GenerateBelt(); 

                }

            }
            else
            {
                if (Random.Range(0, 4) != 1)
                {
                    this.ObjectData.name += "WB";
                    GenerateBelt();

                }
            }


            //GameObject.Destroy(this.planet);

        }


        
        public void CheckLOD()
        {

            Generator.CheckLOD(this);
            if (this.gasObject && this.belt != null)
            {
                foreach (var b in this.belt)
                {
                    b.CheckLOD();
                }
            }   
            

            foreach(var m in this.moons)
            {
                m.CheckLOD();
            }
            

        }

     

        public GameObject Object
        {
            get { return this.planet; }
            set { this.planet = value; }
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
                    r = this.mesh.bounds.size.x / 2f;

                }
                return r;
            }
        }

        public Mesh Mesh
        {
            get
            {
                return this.mesh;
            }
            set
            {
                if (this.Object == null)
                {
                    this.mesh = value;
                    this.ObjectData.Instantiate(this);
                }
                else
                    this.Object.GetComponent<MeshFilter>().mesh = value;
            }
        }

        public Mesh[] LODMeshes
        {
            get { return this.lodMeshes; }
            set { this.lodMeshes = value; }
        }

        public Belt[] Belt
        {
            get { return this.belt; }
        }

        public GameObject Light
        {
            get { return this.light; }
            set { this.light = value; }
        }

        public GData ObjectData
        {
            get { return this.data; }
            set { this.data = value; }
        }

        public GData ParentData
        {
            get { return this.parentData; }
            set { this.parentData = value; }

        }
        public void Destroy()
        {
            if (this.Object != null)
            {
                GameObject.Destroy(this.Object);
            }
            if (this.Light != null)
            {
                GameObject.Destroy(this.Light);
            }

        }

        private void GenerateMoons()
        {
            for (int i = 0; i < moons.Length; i++)
            {
                float mOffset = Random.Range(0.1f, 1f);
                float mRadius = this.radius / Random.Range(50f, 100f);
                
                moons[i] = new Moon(this, this.data.material, mOffset, mRadius);
            }

        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        public struct BeltGenerator : IJobParallelFor
        {
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
                while (distance(position, tmp) < innerRadius
                    || distance(position, tmp) > outerRadius
                    || positions.Contains(pos));

                scales[i] = rand.NextFloat(0.1f, 0.3f);
                pos = (pos - position) * remap(0f, 1f, 0.8f, 1.2f, snoise(pos));
                positions[i] = matrix.MultiplyPoint3x4(pos);
            }
        }

        private void GenerateBelt()
        {

            int count = Random.Range(100, 200);
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
            var jobHandle = job.Schedule(count, 128, default);
            this.belt = new Belt[count];
            jobHandle.Complete();
            
            for (int i = 0; i < results.Length; i++)
            {

                belt[i] = new Belt(this, results[i], scaleResults[i], this.data.material);
            }

            results.Dispose();
            scaleResults.Dispose();
        }
    }

    public class Belt : IGObject
    {
        GameObject beltObject;
        Mesh[] lodMeshes;
        Planet parent;
        GameObject light;
        GData data;
        GData parentData;
        Mesh mesh;

        public Belt(Planet parent, Vector3 position, float scale, Material material)
        {
            this.parent = parent;
            string  name = this.parent.ObjectData.name + "_BeltObj" + GetHashCode();
            this.beltObject = new GameObject();
            this.beltObject.AddComponent<MeshRenderer>().material = material;
            this.beltObject.AddComponent<MeshFilter>();
           

            this.beltObject.transform.SetParent(this.parent.Transform);
            this.beltObject.transform.position = position;
            this.beltObject.transform.localScale = Vector3.one * scale;

            this.parentData = this.parent.ObjectData;
            this.data = new GData
            {
                name = name,
                position = this.beltObject.transform.position,
                scale = this.beltObject.transform.lossyScale,
                material = material,
                parent = this.parentData
            };
            Generator.GenerateMesh(position, ref lodMeshes, name);
            this.mesh = lodMeshes[0];
            Generator.CreateLight(this);

            GameObject.Destroy(this.beltObject);

        }

        public Mesh Mesh
        {
            get
            {
                return this.mesh;
            }
            set
            {
                if (this.Object == null)
                {
                    this.mesh = value;
                    this.ObjectData.Instantiate(this);
                }
                else
                    this.Object.GetComponent<MeshFilter>().mesh = value;
            }
        }


        public GameObject Object
        {
            get { return this.beltObject; }
            set { this.beltObject = value; }
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
            get { return this.mesh.bounds.size.x / 2f; }
        }

        public ParticleSystemRenderer ParticleRenderer { get; }
        public ParticleSystem Particle { get; }
    
        public GData ObjectData
        {
            get { return this.data; }
            set { this.data = value; }
        }

        public GData ParentData
        {
            get { return this.parentData; }
            set { this.parentData = value; }

        }
        public void Destroy()
        {
            if (this.Object != null)
            {
                GameObject.Destroy(this.Object);
            }
            if (this.Light != null)
            {
                GameObject.Destroy(this.Light);
            }

        }
        public float Offset { get; }

        public Mesh[] LODMeshes
        {
            get { return this.lodMeshes; }
        }

        public GameObject Light
        {   get { return this.light; } 
            set { this.light = value; }
        }

        public void CheckLOD() => Generator.CheckLOD(this);

    }


    public class Moon : IGObject
    {

        GameObject moon;
        Planet parent;
        Mesh[] lodMeshes;
        float offset;
        GameObject light;
        GData data;
        GData parentData;
        Mesh mesh;
        public Moon(Planet parent, Material material, float offset, float radius)
        {
            this.parent = parent;
            string name = "Moon" + GetHashCode();
            this.moon = new GameObject();
            this.offset = offset;

            moon.AddComponent<MeshRenderer>().material = material;
            moon.AddComponent<MeshFilter>();
            moon.transform.SetParent(this.parent.Transform);


            this.moon.transform.localScale = Vector3.one * radius;

            Generator.GenerateMesh(this.moon.transform.position, ref lodMeshes, this.moon.name);
            this.mesh = lodMeshes[0];
            this.moon.transform.position = new Vector3(offset, 0f, offset);

            

            this.moon.transform.RotateAround(this.parent.Position,
                this.parent.Transform.up, Random.Range(0f, 360f));
            this.moon.transform.RotateAround(this.parent.Position,
                this.parent.Transform.right, Random.Range(0f, 360f));
            this.moon.transform.RotateAround(this.parent.Position,
                this.parent.Transform.forward, Random.Range(0f, 360f));
            this.parentData = this.parent.ObjectData;
            this.data = new GData
            {
                name = name,
                position = this.moon.transform.position,
                scale = this.moon.transform.lossyScale,
                material = material,
                parent = this.parentData
            };
            Generator.CreateLight(this);

            GameObject.Destroy(this.moon);
        }

        public GameObject Object
        {
            get { return this.moon; }
            set { this.moon = value; }
        }
        public ParticleSystem Particle { get; }
        public Transform Transform
        {
            get { return this.moon.transform; }
        }

        public float Radius
        {
            get { return  this.mesh.bounds.size.x / 2f; }
        }

        public ParticleSystemRenderer ParticleRenderer { get; }
        public bool isGas { get { return false; } }

        public GData ObjectData
        {
            get { return this.data; }
            set { this.data = value; }
        }

        public GData ParentData
        {
            get { return this.parentData; }
            set { this.parentData = value; }

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
                return this.mesh;
            }
            set
            {
                if (this.Object == null)
                {
                    this.mesh = value;
                    this.ObjectData.Instantiate(this);
                }
                else
                    this.Object.GetComponent<MeshFilter>().mesh = value;
            }
        }


        public Mesh[] LODMeshes 
        {
            get { return this.lodMeshes; }
        }

        public GameObject Light
        {
            get { return this.light; }
            set { this.light = value; }
        }

        public void CheckLOD() => Generator.CheckLOD(this);
        public void Destroy()
        {
            if (this.Object != null)
            {
                GameObject.Destroy(this.Object);
            }
            if (this.Light != null)
            {
                GameObject.Destroy(this.Light);
            }

        }
    }



    public static class Generator
    {
        static MeshJobScheduleDelegate meshJob = MeshJob<CubeSphere, MultiStream>.ScheduleParallel;

        
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

            lodMeshes = Application.platform == RuntimePlatform.WebGLPlayer ? new Mesh[3] : new Mesh[5];
            List<JobHandle> jobHandles = new List<JobHandle>();
            var dataArrays = new List<Mesh.MeshDataArray>();
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                dataArrays.Add(Mesh.AllocateWritableMeshData(1));
                Mesh.MeshData meshData = dataArrays.Last()[0];
                Mesh m = new Mesh { name = name + "_LOD" + i };
                var handle = meshJob(m, meshData, resolution, jobHandles.Count > 0 ? jobHandles.Last() : default, passer);
                jobHandles.Add(handle);
                lodMeshes[i] = m;
                resolution *= 2;
            }
            jobHandles.Last().Complete();
            for (int i = 0; i < dataArrays.Count; i++)
            {
                Mesh.ApplyAndDisposeWritableMeshData(dataArrays[i], lodMeshes[i]);

            }


        }

        public static void CreateLight<T>(T obj) where T : IGObject
        {
            float intensity;
            float range;
            float offset;
            Vector3 pos;
            if (obj.GetType() != typeof(Planet))
            {
                offset = 0.996f;
                range = 0.008f;
                intensity = 10000f;
                pos = obj.ParentData.parent.position;
            }
            else
            {
                offset = 0.9f;
                range = 0.02f;
                intensity = 20000f;
                pos = obj.ParentData.position;
            }

            var g = new GameObject("LightTo_" + obj.ObjectData.name);



            Vector3 distance = obj.ObjectData.position - pos;

            g.transform.SetParent(obj.Transform);


            g.transform.position = distance * offset;
            var l = g.AddComponent<Light>();
            l.type = LightType.Spot;
            g.transform.rotation = Quaternion.LookRotation(distance);
            l.range = distance.magnitude * range;
            l.intensity = intensity;
            l.innerSpotAngle = (obj.Radius) * Mathf.Deg2Rad * 180f;

            var d = obj.ObjectData;
            d.light = new GLight
            {
                name = g.name,
                position = g.transform.position,
                rotation = g.transform.rotation,
                intensity = l.innerSpotAngle,
                range = l.range,
                innerSpotAngle = l.innerSpotAngle,
            };
            obj.ObjectData = d;
            GameObject.Destroy(g);


        }

        public static void CheckLOD<T> (T obj) where T : IGObject
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 objToCamera = obj.ObjectData.position - Camera.main.transform.position;
            float dot = Vector3.Dot(camForward, objToCamera);
            float dotOverMag = dot / (camForward.magnitude * objToCamera.magnitude);

            float visible = Mathf.Cos(Mathf.Deg2Rad * 60f);
            float distance = Vector3.Distance(Camera.main.transform.position, obj.ObjectData.position);

            
            if (dotOverMag > visible)
            {


                if (distance > 2000)
                {
                    GameObject.Destroy(obj.Object);
                    GameObject.Destroy(obj.Light);
                    //if (obj.Object == null)
                    //    obj.ObjectData.Instantiate(obj);
                }
                else if (distance > 600f && distance <= 2000)
                {
                    
                    if (!obj.isGas)
                        obj.Mesh = obj.LODMeshes[0];

                    else
                        if (obj.Object == null)
                            obj.ObjectData.Instantiate(obj);
                }
                else if (distance > 400f && distance <= 600f)
                {
                    
                    if (!obj.isGas)
                        obj.Mesh = obj.LODMeshes[1];

                    else
                        if (obj.Object == null)
                            obj.ObjectData.Instantiate(obj);
                }
                else if (distance > 200f && distance <= 400f)
                {
                    if (!obj.isGas)
                        obj.Mesh = obj.LODMeshes[2];

                    else
                        if (obj.Object == null)
                            obj.ObjectData.Instantiate(obj);

                }
                else if (distance > 100f && distance <= 200f)
                {
                    if (!obj.isGas)
                        obj.Mesh = obj.LODMeshes[3];

                    else
                        if (obj.Object == null)
                            obj.ObjectData.Instantiate(obj);

                }
                else if (distance <= 100f)
                {
                    if (!obj.isGas)
                        obj.Mesh= obj.LODMeshes[4];

                    else
                        if (obj.Object == null)
                            obj.ObjectData.Instantiate(obj);


                }


            }
            else
            {
                GameObject.Destroy(obj.Object);
                GameObject.Destroy(obj.Light);
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

            var g = obj.ObjectData;

            g.particle = new GParticle
            {
                initColor = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f),

            };

            obj.ObjectData = g;
        }
    }

    
}
