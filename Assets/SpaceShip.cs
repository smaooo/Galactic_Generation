using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class SpaceShip : MonoBehaviour
{
    [SerializeField]
    private float speed;
    [SerializeField]
    private float mouseSense;
    private Rigidbody rigidbody;
    public bool assigned = false;
    [SerializeField]
    private GameObject seconCam;
    [SerializeField]
    private MeshRenderer mat;

    private void Start()
    {
        
        rigidbody = this.GetComponent<Rigidbody>();

    }

    private void Update()
    {
        //if (!assigned && FindObjectOfType<GalaxyManager>().systemLoaded)
        //{
        //    var planets = new List<Planet>(FindObjectOfType<GalaxyManager>().ss.Planets);
        //    planets.OrderByDescending(p => Vector3.Distance(p.ObjectData.position, Vector3.zero));
        //    print(planets[0].ObjectData.name);
        //    this.transform.position = planets[0].ObjectData.position + new Vector3(100, 0, 100);
        //    this.transform.rotation = Quaternion.LookRotation(FindObjectOfType<GalaxyManager>().ss.Star.ObjectData.position - this.transform.position);
        //    assigned = true;
        //}

        //seconCam.transform.position = new Vector3(this.transform.position.x + 0.42f, this.transform.position.y, this.transform.position.z + 0.268f);
        //seconCam.transform.rotation = Quaternion.LookRotation(-this.transform.right, this.transform.up);
    }



    private void FixedUpdate()
    {
            Move();
        //if (assigned)
    }

    private void Move()
    {
        Vector3 velocity = Input.GetAxis("Horizontal") * this.transform.right + Input.GetAxis("Vertical") * this.transform.forward;
        Vector3 torque = Input.GetAxis("Horizontal")  * this.transform.up *0.001f+ Input.GetAxis("Mouse X") * this.transform.forward + Input.GetAxis("Mouse Y") * -this.transform.right;
        rigidbody.AddForce(velocity * speed, ForceMode.Acceleration);
        rigidbody.AddTorque(torque * mouseSense);
        
        mat.material.SetColor("_EmissionColor", Color.white * remap(0, 1, 10, 100, Mathf.Abs(velocity.magnitude)));
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    
}
