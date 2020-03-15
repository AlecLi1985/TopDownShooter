using UnityEngine;

public class SpawnEnemies : MonoBehaviour
{

    public GameObject enemyObject;
    public float spawnRadius;
    public int spawnNumber;


    public void SpawnEnemyGroup()
    {
        if (enemyObject != null)
        {
            for (int i = 0; i < spawnNumber; i++)
            {
                Vector3 spawnPosition = transform.position + (Random.insideUnitSphere * spawnRadius);
                spawnPosition.y = 1f;

                Instantiate(enemyObject, spawnPosition, Quaternion.identity, transform);
            }
        }
    }

    public void ClearEnemies()
    {
        for(int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
