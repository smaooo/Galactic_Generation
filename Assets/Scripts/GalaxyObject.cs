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
                if (p.isGasPlanet)
                {
                    for (int j = 0; j < this.planets[i].Belt.Length; j++)
                    {
                        var b = p.Belt[j];
                        relations.Add(new Relations
                        {
                            id = j,
                            type = "BELT",
                            radius = b.Radius,
                            transform = b.BeltTransform,
                            planet = p
                        });
                    }

                }
            }

            Generator.GenerateLights(ref this.lights, this, relations);

            
        }

        public void CheckLOD()
        {
            Generator.CheckLOD(obj: this.star.StarObject, gasPlanet: this.star.ParticleRenderer, radius: this.Radius);
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
      
    }


    public class Star
    {
        GameObject star;
        Color initColor;

        public Star(GameObject starPrefab, Transform parent)
        {
            this.star = GameObject.Instantiate(starPrefab, parent);
            //this.star.isStatic = true;

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

        public ParticleSystemRenderer ParticleRenderer
        {
            get { return this.star.GetComponent<ParticleSystemRenderer>(); }
        }
        
    }
    public class Planet
    {
        GameObject planet;
        Moon[] moons;
        Material material;
        float radius;
        float offset;
        Star star;
        SolarSystem solarSystem;
        Mesh[] lodMeshes;
        Belt[] belt;
        bool gasObject;

        public Planet(SolarSystem solarSystem, Material material, float offset, float radius, Transform parent)
        {
            this.planet = new GameObject("Planet" + GetHashCode());
            //this.planet.isStatic = true;
            this.material = material;
            this.radius = radius;
            this.offset = offset;
            this.solarSystem = solarSystem;
            this.star = this.solarSystem.Star;
            this.gasObject = false;

            this.planet.AddComponent<MeshRenderer>().material = material;
            this.planet.AddComponent<MeshFilter>();

            this.planet.transform.SetParent(parent);

            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);
            Generator.GenerateMesh(this.planet.transform, ref lodMeshes);

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
            //this.planet.isStatic = true;
            this.planet.name = "GasPlanet" + GetHashCode();
            this.material = material;
            this.solarSystem = solarSystem;
            this.star = this.solarSystem.Star;
            this.gasObject = true;
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


        private void SetupGasPlanet()
        {

        }

        public void CheckLOD()
        {
            if (!this.gasObject)
                Generator.CheckLOD(lodMeshes: this.lodMeshes, obj: this.planet, radius: this.Radius);
            else
            {

                Generator.CheckLOD(obj: this.planet, gasPlanet: this.planet.GetComponent<ParticleSystemRenderer>(), radius: this.Radius);
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

        public bool isGasPlanet
        {
            get { return this.gasObject; }
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

        public Belt[] Belt
        {
            get { return this.belt; }
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
            this.beltObject.AddComponent<MeshRenderer>().material = material;
            this.beltObject.AddComponent<MeshFilter>();



            Generator.GenerateMesh(this.beltObject.transform, ref lodMeshes);

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
        public float Radius
        {
            get { return this.beltObject.GetComponent<MeshRenderer>().bounds.size.x / 2f; }
        }

        public void CheckLOD() => Generator.CheckLOD(this.lodMeshes, this.beltObject, this.Radius);

    }


    public class Moon
    {

        GameObject moon;
        Planet planet;
        Mesh[] lodMeshes;

        public Moon(Planet planet, Material material, float offset, float radius)
        {
            this.moon = new GameObject(planet.PlanetObject.name + "_Moon" + GetHashCode());
            //this.moon.isStatic = true;
            this.planet = planet;

            moon.AddComponent<MeshRenderer>().material = material;
            moon.AddComponent<MeshFilter>();
            moon.transform.SetParent(planet.PlanetObject.transform);


            this.moon.transform.localScale = Vector3.one * radius;

            Generator.GenerateMesh(this.moon.transform, ref lodMeshes);
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

        public void CheckLOD() => Generator.CheckLOD(this.lodMeshes, this.moon, this.Radius);

    }

    public static class Generator
    {
        static MeshJobScheduleDelegate meshJob = MeshJob<CubeSphere, SingleStream>.ScheduleParallel;

        public static void GenerateMesh(Transform transform, ref Mesh[] lodMeshes)
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
                Mesh m = new Mesh { name = transform.gameObject.name + "_LOD" + i };
                meshJob(m, meshData, resolution, default, passer).Complete();
                lodMeshes[i] = m;
                resolution *= 2;
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, lodMeshes[i]);
            }

      
            transform.GetComponent<MeshFilter>().mesh = lodMeshes[0];

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
        
        
        public static void CheckLOD(Mesh[] lodMeshes = null, GameObject obj = null, float radius = 0f, ParticleSystemRenderer gasPlanet = null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 objToCamera = obj.transform.position - Camera.main.transform.position;
            float dot = Vector3.Dot(camForward, objToCamera);
            float dotOverMag = dot / (camForward.magnitude * objToCamera.magnitude);

            float visible = Mathf.Cos(Mathf.Deg2Rad * 60f);
            float distance = Vector3.Distance(Camera.main.transform.position, obj.transform.position);
            
            if (lodMeshes == null && gasPlanet != null)
            {
                
                if (dotOverMag > visible)
                {
                    if (distance > 100 * radius)
                        gasPlanet.enabled = false;

                    else
                        gasPlanet.enabled = true;
                }
                else
                    gasPlanet.enabled = false;

                return;
            }
            if (dotOverMag > visible)
            {

              
                if (distance > 1000f)
                {
                    obj.GetComponent<MeshFilter>().mesh = null;
                    obj.GetComponent<MeshRenderer>().enabled = false;
                }
                else if (distance > 600f && distance <= 1000f)
                {
                    obj.SetActive(true);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                    obj.GetComponent<MeshFilter>().mesh = lodMeshes[0];
                }
                else if (distance > 400f && distance <= 600f)
                {
                    obj.SetActive(true);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                    obj.GetComponent<MeshFilter>().mesh = lodMeshes[1];
                }
                else if (distance > 200f && distance <= 400f)
                {
                    obj.SetActive(true);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                    obj.GetComponent<MeshFilter>().mesh = lodMeshes[2];
                }
                else if (distance > 100f && distance <= 200f)
                {
                    obj.SetActive(true);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                    obj.GetComponent<MeshFilter>().mesh = lodMeshes[3];
                }
                else if (distance <= 100f)
                {
                    obj.SetActive(true);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                    obj.GetComponent<MeshFilter>().mesh = lodMeshes[4];
                }

            }
            else
            {
                obj.GetComponent<MeshFilter>().mesh = null;
                obj.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        
    }
}
