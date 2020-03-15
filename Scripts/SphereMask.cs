using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereMask : MonoBehaviour
{
    Material material;
    Vector4 sphereParamsVector;

    public Transform sphereCentre;
    public float sphereRadius = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = renderer.material;

        sphereParamsVector = new Vector4();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(sphereCentre.position);
        sphereParamsVector = sphereCentre.position;
        sphereParamsVector.w = sphereRadius;

        material.SetVector("_SphereParams", sphereParamsVector);

    }
}
