using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnExplosions : MonoBehaviour
{
    public GameObject explosionObject;
    public float minExplosionDelay = 1f;
    public float maxExplosionDelay = 2f;
    public float spawnExplosionRadius = 10f;

    public bool startSpawningTheExplosions = false;

    float currentExplosionTime = 0f;
    float currentExplosionDelay = 0f;
    // Update is called once per frame
    void Update()
    {
        if(startSpawningTheExplosions)
        {
            if (currentExplosionTime == 0f)
            {
                currentExplosionDelay = Random.Range(minExplosionDelay, maxExplosionDelay);

                if (explosionObject != null)
                {
                    Instantiate(explosionObject, transform.position + (Random.insideUnitSphere * spawnExplosionRadius), Quaternion.identity);
                }
            }

            currentExplosionTime += Time.deltaTime;

            if (currentExplosionTime > currentExplosionDelay)
            {
                currentExplosionTime = 0f;
            }
        }

    }

    public void StartSpawningTheExplosions(bool start)
    {
        startSpawningTheExplosions = start;
    }
}
