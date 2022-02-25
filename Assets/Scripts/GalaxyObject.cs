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
    public class SolarSystem
    {
        Planet[] planets;
        Star star;
        GameObject[] lights;

        public struct Relations
        {
            public int id;
            public string type;
            public float radius;
            public Transform transform;
            public Planet planet;
        }

        public SolarSystem(Transform parent, Material planetMaterial, GameObject gas)
        {
            this.star = new Star(gas, parent);
            
            this.planets = new Planet[Random.Range(1, 10)];
            float distance = 0f;

            for (int i = 0; i < this.planets.Length; i++)
            {
                distance += Random.Range(10000f, 10000f);

                this.planets[i] = Random.Range(0f, 1f) <= 0.3f && i > 2 ? new Planet(this, gas, planetMaterial, distance, Random.Range(0.5f, 5f), parent)
                    : new Planet(this, planetMaterial, distance, Random.Range(5f, 40f), parent);

            }



            List<Relations> relations = new List<Relations>();
            for (int i = 0; i < this.planets.Length; i++)
            {
                var p = this.planets[i];
                relations.Add(new Relations
                {
                    id = i,
                    type = "PLANET",
                    radius = p.Radius,
                    transform = p.PlanetTransform
                });
                for (int j = 0; j < this.planets[i].Moons.Length; j++)
                {
                    var m = p.Moons[j];
                    relations.Add(new Relations
                    {
                        id = j,
                        type = "MOON",
                        radius = m.Radius,
                        transform = m.MoonTransform,
                        planet = p
                    });
                }
            }

            Generator.GenerateLights(ref this.lights, this, relations);

            //StaticOcclusionCulling.Compute();


        }


        public Planet[] Planets
        {
            get { return this.planets; }
        }

        public Star Star
        {
            get { return this.star; }
        }
      
    }


    public class Star
    {
        GameObject star;
        Color initColor;

        public Star(GameObject starPrefab, Transform parent)
        {
            this.star = GameObject.Instantiate(starPrefab, parent);
            this.star.isStatic = true;
            //var lod = this.star.AddComponent<LODGroup>();

            //LOD l1 = new LOD(1f,);
            //LOD l2 = new LOD();
            //LOD l3 = new LOD();

            //lod.SetLODs(new LOD[] { l1, l2, l3 });
            SetupStar();
        }

        private void SetupStar()
        {
            var particle = this.star.GetComponent<ParticleSystem>();
            var main = particle.main;
            
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
            this.initColor = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);
            main.startColor = this.initColor;
            
            this.star.GetComponent<ParticleSystemRenderer>().material.SetVector("_EmissionColor", 
                new Vector4(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b,
                4f));
            Gradient colorGrad = new Gradient();
            var colorOverL = particle.colorOverLifetime;
            Color color1 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f,1f), 1f);
            Color color2 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);

            colorGrad.SetKeys(new GradientColorKey[] {
            new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, 0.5f)},
                new GradientAlphaKey[] {
                new GradientAlphaKey(1f,0f), new GradientAlphaKey(Random.Range(0f, 0.2f),Random.Range(0.4f, 0.8f))});
            colorOverL.color = colorGrad;

            var colorBySpeed = particle.colorBySpeed;

            color1 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);
            color2 = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);

            colorGrad.SetKeys(new GradientColorKey[] {
            new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, Random.Range(0.4f, 0.8f))},
                new GradientAlphaKey[] {
                new GradientAlphaKey(1f,0f), new GradientAlphaKey(Random.Range(0f, 0.2f),Random.Range(0.4f, 0.8f))});

            colorBySpeed.color = colorGrad;
        }

        public GameObject StarObject
        {
            get { return this.star; }
        }

        public Color Color
        {
            get { return this.initColor; }
        }
        public Transform StarTransform
        {
            get { return this.star.transform; }
        }

      
        
    }
    public class Planet
    {


        Mesh mesh;
        GameObject planet;
        Moon[] moons;
        Material material;
        float radius;
        float offset;
        Star star;
        SolarSystem solarSystem;
        Mesh[] lodMeshes;
        Belt[] belt;
        
        public Planet(SolarSystem solarSystem, Material material, float offset, float radius, Transform parent)
        {
            this.planet = new GameObject("Planet" + GetHashCode());
            this.planet.isStatic = true;
            this.mesh = new Mesh { name = this.planet.name};
            this.material = material;
            this.radius = radius;
            this.offset = offset;
            this.solarSystem = solarSystem;
            this.star = this.solarSystem.Star;
            
            this.planet.AddComponent<MeshRenderer>().material = material;
            this.planet.AddComponent<MeshFilter>().mesh = this.mesh;
            this.planet.transform.SetParent(parent);
            

            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);
            Generator.GenerateMesh(this.mesh, this.planet.transform, ref lodMeshes);

            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.up, Random.Range(0f,360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.right, Random.Range(0f,360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.forward, Random.Range(0f,360f));

            int numberOfMoons = radius > 1 ? (int)(Random.Range(5, 10)) : (int)(Random.Range(0, 5) * radius);

            this.moons = new Moon[numberOfMoons];
            GenerateMoons();
            
        }

        public Planet(SolarSystem solarSystem, GameObject gas, Material material, float offset, float radius, Transform parent)
        {
            this.planet = GameObject.Instantiate(gas, parent);
            this.planet.isStatic = true;
            this.planet.name = "GasPlanet" + GetHashCode();
            this.material = material;
            this.solarSystem = solarSystem;
            this.star = this.solarSystem.Star;

            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);

            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.up, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.right, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.forward, Random.Range(0f, 360f));
            this.radius = this.planet.GetComponent<ParticleSystemRenderer>().bounds.size.x / 2;
            int numberOfMoons = (int)(Random.Range(5, 10));

            this.moons = new Moon[numberOfMoons];

            GenerateMoons();

            GenerateBelt();

        }

      


        public GameObject PlanetObject
        {
            get { return this.planet; }
        }

        public Moon[] Moons
        {
            get { return this.moons; }
        }

        public Transform PlanetTransform
        {
            get { return this.planet.transform; }
        }

        public float Offset
        {
            get { return this.offset; }
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
        public struct BeltGenerator : IJobFor
        {
            public NativeArray<float3> positions;
            public NativeArray<float> scales;
            public float innerRadius;
            public float outerRadius;
            public Matrix4x4 matrix;
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
            int count = Random.Range(1000,2000);
            float innerRingRadius = this.radius * Mathf.Log10(count) / Random.Range(2f,2.5f);
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
            job.Schedule(count, default).Complete();
            this.belt = new Belt[count];
            for (int i = 0; i < results.Length; i++)
            {
                belt[i] = new Belt(this, results[i], scaleResults[i], this.material);

                
            }


            results.Dispose();
            scaleResults.Dispose();

        }
    }

    public class Belt
    {
        GameObject beltObject;
        Mesh[] lodMeshes;

        public Belt(Planet planet, Vector3 position, float scale, Material material)
        {
            this.beltObject = new GameObject(planet.PlanetObject.name + "BeltObj" + GetHashCode());
            //p.isStatic = true;
            Mesh m = new Mesh { name = this.beltObject.name };
            this.beltObject.AddComponent<MeshRenderer>().material = material;
            this.beltObject.AddComponent<MeshFilter>().mesh = m;



            Generator.GenerateMesh(m, this.beltObject.transform, ref lodMeshes);

            this.beltObject.transform.position = position;
            this.beltObject.transform.SetParent(planet.PlanetTransform);
            this.beltObject.transform.localScale = Vector3.one * scale;
        }

        public GameObject BeltObject
        {
            get { return this.beltObject; }
        }
        public Transform BeltTransform
        {
            get { return this.beltObject.transform; }
        }
    }


    public class Moon
    {

        Mesh mesh;
        GameObject moon;
        Planet planet;
        Mesh[] lodMeshes;

        public Moon(Planet planet, Material material, float offset, float radius)
        {
            this.moon = new GameObject(planet.PlanetObject.name + "_Moon" + GetHashCode());
            this.moon.isStatic = true;
            this.mesh = new Mesh { name = this.moon.name };
            this.planet = planet;

            moon.AddComponent<MeshRenderer>().material = material;
            moon.AddComponent<MeshFilter>().mesh = this.mesh;
            moon.transform.SetParent(planet.PlanetObject.transform);


            this.moon.transform.localScale = Vector3.one * radius;

            Generator.GenerateMesh(this.mesh, this.moon.transform, ref lodMeshes);
            this.moon.transform.position = new Vector3(offset, 0f, offset);

            Transform pl = planet.PlanetObject.transform;

            this.moon.transform.RotateAround(pl.position, pl.up, Random.Range(0f, 360f));
            this.moon.transform.RotateAround(pl.position, pl.right, Random.Range(0f, 360f));
            this.moon.transform.RotateAround(pl.position, pl.forward, Random.Range(0f, 360f));
        }

        public GameObject MoonObject
        {
            get { return this.moon; }
        }

        public Transform MoonTransform
        {
            get { return this.moon.transform; }
        }

        public float Radius
        {
            get { return  this.moon.GetComponent<MeshRenderer>().bounds.size.x / 2f; }
        }
    }

    public static class Generator
    {
        static MeshJobScheduleDelegate meshJob = MeshJob<CubeSphere, SingleStream>.ScheduleParallel;

        public static void GenerateMesh(Mesh mesh, Transform transform, ref Mesh[] lodMeshes)
        {
            NoiseLayer[] noiseLayers = new NoiseLayer[4];

            noiseLayers[0] = new NoiseLayer
            {
                active = true,
                useFirstLayerAsMask = false,
                noiseSettings = new NoiseSettings
                {
                    center = float3(
                            snoise(transform.position) * Random.Range(-100f, 100f),
                            snoise(transform.position) * Random.Range(-100f, 100f),
                            snoise(transform.position) * Random.Range(-100f, 100f)),
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
                            snoise(transform.position) * Random.Range(-100f, 100f),
                            snoise(transform.position) * Random.Range(-100f, 100f),
                            snoise(transform.position) * Random.Range(-100f, 100f)),
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
                            snoise(transform.position) * Random.Range(-100f, 100f),
                            snoise(transform.position) * Random.Range(-100f, 100f),
                            snoise(transform.position) * Random.Range(-100f, 100f)),
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

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            Passer passer = new Passer
            {
                nl1 = noiseLayers[0].active ? noiseLayers[0] : new NoiseLayer(),
                nl2 = noiseLayers[1].active ? noiseLayers[1] : new NoiseLayer(),
                nl3 = noiseLayers[2].active ? noiseLayers[2] : new NoiseLayer(),
                nl4 = noiseLayers[3].active ? noiseLayers[3] : new NoiseLayer()
            };

            int resolution = 8;
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                meshJob(mesh, meshData, resolution, default, passer).Complete();
                lodMeshes[i] = mesh;
                resolution *= 2;
            }
            
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, lodMeshes[0]);

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

                lights[i] = new GameObject("LightTo_" + r.transform.gameObject.name);
                Vector3 distance = r.transform.position - ss.Star.StarTransform.position;
                lights[i].transform.SetParent(ss.Star.StarTransform);

                lights[i].transform.position = distance * offset;
                var l = lights[i].AddComponent<Light>();
                l.type = LightType.Spot;
                lights[i].transform.rotation = Quaternion.LookRotation(distance);
                l.range = distance.magnitude * range;
                l.intensity = intensity;
                l.innerSpotAngle = (r.radius) * Mathf.Deg2Rad * 180f;
                   
            }

           

        }


        
    }
}
