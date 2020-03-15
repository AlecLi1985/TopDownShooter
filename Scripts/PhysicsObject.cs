using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    public float health = 100f;
    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void AddForce(float force, Vector3 forceDirection, Vector3 forcePosition, ForceMode forceMode)
    {
        rb.AddForceAtPosition(forceDirection * force, forcePosition, forceMode);
    }

    public void AddExplosionForce(float damage, float force, Vector3 forcePosition, float forceRadius, ForceMode forceMode)
    {
        rb.AddExplosionForce(force, forcePosition, forceRadius, 1f, ForceMode.Force);
        //health -= damage;

        if (health <= 0f)
        {
            //break into pieces then destroy the pieces
            Destroy(gameObject);
        }
    }

}
