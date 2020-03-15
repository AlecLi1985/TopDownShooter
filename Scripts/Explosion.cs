using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public float lifeTime = 2f;
    float currentTime = 0f;

    void Update()
    {
        currentTime += Time.deltaTime;
        if(currentTime > lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
