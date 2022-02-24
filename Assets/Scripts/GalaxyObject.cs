using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;


namespace GalaxyObject
{
    public class Planet
    {


        Mesh mesh;
        GameObject planet;
        Moon[] moons;
        Material material;
        float radius; 

        public Planet(Material material, float offset, float radius, Transform star, int numberOfMoons)
        {
            this.planet = new GameObject("Planet" + GetHashCode());
            this.mesh = new Mesh { name = this.planet.name};
            this.material = material;
            this.radius = radius;

            this.planet.AddComponent<MeshRenderer>().material = material;
            this.planet.AddComponent<MeshFilter>().mesh = this.mesh;
            this.planet.transform.SetParent(star);
            
            Generator.GenerateMesh(this.mesh);

            this.planet.transform.localScale = Vector3.one * radius;

            float actRadius = planet.GetComponent<MeshRenderer>().bounds.size.x / 2;
            this.planet.transform.position = new Vector3(offset, 0f, offset);

            this.planet.transform.RotateAround(star.transform.position, star.transform.up, Random.Range(0f,360f));
            this.planet.transform.RotateAround(star.transform.position, star.transform.right, Random.Range(0f,360f));
            this.planet.transform.RotateAround(star.transform.position, star.transform.forward, Random.Range(0f,360f));
            
            this.moons = new Moon[numberOfMoons];
            GenerateMoons();

        }

        public GameObject PlanetObject
        {
            get { return this.planet; }
        }

        public Moon[] Moons
        {
            get { return this.moons; }
        }

        public void GenerateMoons()
        {
            for (int i = 0; i < moons.Length; i++)
            {
                float offset = Random.Range(0.01f,0.1f);
                float radius = this.radius / Random.Range(5f, 10f);
                
                moons[i] = new Moon(this, this.material, offset, radius);
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
    }

    public static class Generator
    {
        static MeshJobScheduleDelegate meshJob = MeshJob<CubeSphere, SingleStream>.ScheduleParallel;

        public static void GenerateMesh(Mesh mesh)
        {
            Debug.Log(mesh.name);
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
