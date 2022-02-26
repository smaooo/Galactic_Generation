using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;
using UnityEngine.UI;

public class GalaxyManager : MonoBehaviour
{
    [SerializeField]
    private Material planetMaterial;
    [SerializeField]
    private GameObject starPrefab;
    SolarSystem ss;
    // Start is called before the first frame update
    void Start()
    {
        ss = new SolarSystem(this.transform, planetMaterial, starPrefab);            
        foreach(var p in ss.Planets)
        {
            FindObjectOfType<Text>().text += p.PlanetObject.name + "\n";
        }
    }

    // Update is called once per frame
    void Update()
    {
        ss.CheckLOD();
    }
}
