using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTest : MonoBehaviour
{

    GameObject g;
    Transform t;
    private void Start()
    {
        g = new GameObject();

        Transform t = g.transform;
        t.position = Vector3.one * 10;
     

        print(t.position);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Destroy(g);
            print(t.position);
        }
    }
}
