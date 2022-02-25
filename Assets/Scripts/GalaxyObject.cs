using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;


namespace GalaxyObject
{
    public class SolarSystem
    {
        Planet[] planets;
        Star star;
        GameObject[] lights;

        public SolarSystem(Transform parent, Material planetMaterial, GameObject gas)
        {
            this.star = new Star(gas, parent);

            this.planets = new Planet[Random.Range(1, 10)];
            float distance = 0f;

            for (int i = 0; i < this.planets.Length; i++)
            {
                distance += Random.Range(10000f, 10000f);

                this.planets[i] = Random.Range(0f, 1f) <= 0.3f && i > 2 ? new Planet(gas, planetMaterial, distance, Random.Range(0.5f, 5f), parent, this.star)
                    : new Planet(planetMaterial, distance, Random.Range(5f, 40f), parent, this.star);

            }

            CreateLights();
        }

        
        public Planet[] Planets
        {
            get { return this.planets; }
        }

        private void CreateLights()
        {
            this.lights = new GameObject[this.planets.Length];

            for (int i = 0; i < this.planets.Length; i++)

            {
                this.lights[i] = new GameObject("LightTo_" + this.planets[i].PlanetObject.name);

                this.lights[i].transform.SetParent(this.star.StarTransform);
                this.lights[i].transform.position = (this.planets[i].PlanetTransform.position - this.star.StarTransform.position) * 0.98f;
                var l = this.lights[i].AddComponent<Light>();
                l.type = LightType.Spot;
                this.lights[i].transform.rotation = Quaternion.LookRotation(this.planets[i].PlanetTransform.position - this.star.StarTransform.position);
                l.range = (this.planets[i].PlanetTransform.position - this.star.StarTransform.position).magnitude * 0.04f;
                l.intensity = 20000f;
                l.innerSpotAngle = this.planets[i].Radius * Mathf.Deg2Rad * 180f;
            }
            
        }
    }


    public class Star
    {
        GameObject star;

        public Star(GameObject starPrefab, Transform parent)
        {
            this.star = GameObject.Instantiate(starPrefab, parent);

            SetupStar();
        }

        private void SetupStar()
        {
            var particle = this.star.GetComponent<ParticleSystem>();
            var main = particle.main;
            
            float colorMin = 0f;
            float colorMax = 0f;

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
            main.startColor = Color.HSVToRGB(Random.Range(colorMin, colorMax), Random.Range(0.5f, 1f), 1f);
            
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
        GameObject[] lights;
        Star star;
        public Planet(Material material, float offset, float radius, Transform parent, Star star)
        {
            this.planet = new GameObject("Planet" + GetHashCode());
            this.mesh = new Mesh { name = this.planet.name};
            this.material = material;
            this.radius = radius;
            this.offset = offset;
            this.star = star;

            this.planet.AddComponent<MeshRenderer>().material = material;
            this.planet.AddComponent<MeshFilter>().mesh = this.mesh;
            this.planet.transform.SetParent(parent);
            
            Generator.GenerateMesh(this.mesh);

            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);

            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.up, Random.Range(0f,360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.right, Random.Range(0f,360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.forward, Random.Range(0f,360f));

            int numberOfMoons = radius > 1 ? (int)(Random.Range(5, 10)) : (int)(Random.Range(0, 5) * radius);

            this.moons = new Moon[numberOfMoons];
            GenerateMoons();
            CreateLights();

        }

        public Planet(GameObject gas, Material material, float offset, float radius, Transform parent, Star star)
        {
            this.planet = GameObject.Instantiate(gas, parent);
            this.planet.name = "GasPlanet" + GetHashCode();
            this.radius = radius;
            this.material = material;
            this.star = star;

            this.planet.transform.localScale = Vector3.one * radius;

            this.planet.transform.position = new Vector3(offset, 0f, offset);

            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.up, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.right, Random.Range(0f, 360f));
            this.planet.transform.RotateAround(star.StarTransform.position, star.StarTransform.forward, Random.Range(0f, 360f));

            int numberOfMoons = (int)(Random.Range(5, 10));

            this.moons = new Moon[numberOfMoons];

            GenerateMoons();

            CreateLights();


        }

        private void CreateLights()
        {
            this.lights = new GameObject[this.moons.Length];
            
            for (int i = 0; i < this.moons.Length; i++)
            {
                this.lights[i] = new GameObject("LightTo_" + this.moons[i].MoonObject.name);

                Debug.Log(lights[i]);
                this.lights[i].transform.SetParent(this.planet.transform);
                this.lights[i].transform.position = (this.moons[i].MoonTransform.position - this.star.StarTransform.position) * 0.995f;
                var l = this.lights[i].AddComponent<Light>();
                l.type = LightType.Spot;
                this.lights[i].transform.rotation = Quaternion.LookRotation(this.moons[i].MoonTransform.position - this.star.StarTransform.position);
                l.range = (this.moons[i].MoonTransform.position - this.star.StarTransform.position).magnitude * 0.01f;
                l.intensity = 10000f;
                l.innerSpotAngle = this.moons[i].Radius * Mathf.Deg2Rad * 180f;
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
    }

    public class Moon
    {

        Mesh mesh;
        GameObject moon;

        public Moon(Planet planet, Material material, float offset, float radius)
        {
            this.moon = new GameObject(planet.PlanetObject.name + "_Moon" + GetHashCode());
            this.mesh = new Mesh { name = this.moon.name };

            moon.AddComponent<MeshRenderer>().material = material;
            moon.AddComponent<MeshFilter>().mesh = this.mesh;
            moon.transform.SetParent(planet.PlanetObject.transform);
            Generator.GenerateMesh(this.mesh);


            this.moon.transform.localScale = Vector3.one * radius;

            float actRadius = this.moon.GetComponent<MeshRenderer>().bounds.size.x / 2;
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

        public static void GenerateMesh(Mesh mesh)
        {
            NoiseLayer[] noiseLayers = new NoiseLayer[4];

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            Passer passer = new Passer
            {
                nl1 = noiseLayers[0].active ? noiseLayers[0] : new NoiseLayer(),
                nl2 = noiseLayers[1].active ? noiseLayers[1] : new NoiseLayer(),
                nl3 = noiseLayers[2].active ? noiseLayers[2] : new NoiseLayer(),
                nl4 = noiseLayers[3].active ? noiseLayers[3] : new NoiseLayer()
            };


            meshJob(mesh, meshData, ProceduralSettings.Resolution, default, passer).Complete();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        }
    }
}
