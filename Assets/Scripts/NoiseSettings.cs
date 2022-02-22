using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Noise Settings", menuName = "Noise Settings")]
public class NoiseSettings : ScriptableObject
{
    public float strength = 1f;
    public float roughness = 1f;
    public Vector3 center;
}
