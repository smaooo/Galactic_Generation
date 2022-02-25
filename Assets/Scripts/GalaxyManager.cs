using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;

public class GalaxyManager : MonoBehaviour
{
    [SerializeField]
    private Material planetMaterial;
    [SerializeField]
    private GameObject starPrefab;

    // Start is called before the first frame update
    void Start()
    {
        SolarSystem ss = new SolarSystem(this.transform, planetMaterial, starPrefab);            
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
