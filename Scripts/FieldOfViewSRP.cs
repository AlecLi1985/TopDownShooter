using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfViewSRP : MonoBehaviour
{
    public MeshFilter meshFilter;
    public float fov = 90f;
    public float fovDistance = 50f;
    public int rayCount = 2;
    public LayerMask collisionMask;

    Mesh mesh;

    float angle = 0f;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        float angleIncrement = fov / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2]; //ray end points plus origin to origin end point
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];

        Vector3 origin = Vector3.zero;

        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 angleVector = DirectionFromAngle(angle);

            Vector3 v;

            RaycastHit rayHit;
            if (Physics.Raycast(origin, angleVector, out rayHit, fovDistance, collisionMask))
            {
                v = rayHit.point;
            }
            else
            {
                v = origin + angleVector * fovDistance;
            }
            vertices[vertexIndex] = v;

            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;

                triangleIndex += 3;
            }

            vertexIndex++;

            angle += angleIncrement; //minus rotate clockwise, add rotate counter clockwise
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

    public Vector3 DirectionFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private void OnDrawGizmos()
    {
        //float angle = 0f;
        //float angleIncrement = fov / rayCount;

        //for (int i = 0; i <= rayCount; i++)
        //{
        //    Vector3 angleVector = DirectionFromAngle(angle);

        //    Gizmos.DrawLine(transform.position, transform.position + angleVector * fovDistance);

        //    angle += angleIncrement; //minus rotate clockwise, add rotate counter clockwise
        //}
    }
}
