using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Visualization))]
public class VisHelp : Editor
{
    public void OnSceneGUI()
    {
        Visualization v = target as Visualization;
        
        foreach (var t in v.verts)
        {
            Handles.Label(t, v.verts.IndexOf(t).ToString() + " " + t.ToString());
        }
    }
}
