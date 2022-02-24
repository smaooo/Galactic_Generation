using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarSystem : MonoBehaviour
{
    [SerializeField]
    private GameObject starPre;
    private ParticleSystem starParticle;
    private GameObject star;

    private void Start()
    {
        
        star = new GameObject();
        
        star = Instantiate(starPre, this.transform);
        
        SetupStarParticle(starPre.GetComponent<ParticleSystem>());

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
