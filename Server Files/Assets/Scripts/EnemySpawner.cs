using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float cooldown = 3f;

    private void Start()
    {
        StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        // Wait for spawn cooldown
        yield return new WaitForSeconds(cooldown);

        // If there aren't the max number of enemies
        if (Enemy.enemies.Count < Enemy.maxEnemies)
        {
            // Spawn a new enemy
            NetworkManager.instance.InstantiateEnemy(transform.position);
        }

        // Loop
        StartCoroutine(SpawnEnemy());
    }
}
