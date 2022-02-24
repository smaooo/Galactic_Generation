using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;

public class SolarSystem : MonoBehaviour
{
    [SerializeField]
    private GameObject starPre;
    private ParticleSystem starParticle;
    private GameObject star;
    [SerializeField]
    private Material planetMaterial;

    private void Start()
    {
        

        
        star = Instantiate(starPre, this.transform);
        
        SetupStarParticle(starPre.GetComponent<ParticleSystem>());

        //GameObject planet = new GameObject();
        //var pM = planet.AddComponent<ProceduralMesh>();
        //pM.meshType = ProceduralMesh.MeshType.UVSphere;
        GeneratePlanets(star.transform, planetMaterial);
        
    }
    
    private static void GeneratePlanets(Transform star, Material planetMaterial)
    {
        int num = Random.Range(1, 10);
        float distance = 0f;
        
        for (int i = 0; i < num; i++)
        {
            distance += Random.Range(100f, 500f);
            float radius = Random.Range(0.5f, 4f);
            int numMoons = 0;
            if (radius > 1)
            {
                numMoons = (int)(Random.Range(5, 10));

            }
            else
            {
                numMoons = (int)(Random.Range(0, 5) * radius);

            }
            Debug.Log(numMoons);
            Planet planet = new Planet(planetMaterial, distance, radius, star, numMoons);
        }
    }
    private static void SetupStarParticle(ParticleSystem particle)
    {
        var main = particle.main;
        float hue = Random.Range(0f, 1f);
        main.startColor = Color.HSVToRGB(hue, 1f, 1f);
        Gradient colorGrad = new Gradient();
        
        var colorOverL = particle.colorOverLifetime;
        Color color1 = Color.HSVToRGB(Random.Range(hue - 0.4f >= 0f ? hue - 0.4f : 0f, hue + 0.4f <= 1f ? hue + 0.4f : 1f), 1f, 1f);
        Color color2 = Color.HSVToRGB(Random.Range(hue - 0.4f >= 0f ? hue - 0.4f : 0f, hue + 0.4f <= 1f ? hue + 0.4f : 1f), 1f, 1f);
        
        colorGrad.SetKeys(new GradientColorKey[] { 
            new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, 0.5f)},
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,0.5f)});
        colorOverL.color = colorGrad;

        var colorBySpeed = particle.colorBySpeed;

        color1 = Color.HSVToRGB(Random.Range(hue - 0.4f >= 0f ? hue - 0.4f : 0f, hue + 0.4f <= 1f ? hue + 0.4f : 1f), 1f, 1f);
        color2 = Color.HSVToRGB(Random.Range(hue - 0.4f >= 0f ? hue - 0.4f : 0f, hue + 0.4f <= 1f ? hue + 0.4f : 1f), 1f, 1f);

        colorGrad.SetKeys(new GradientColorKey[] {
            new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, 0.5f)},
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,0.5f)});

        colorBySpeed.color = colorGrad;

        
    }


}
