using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    // Global variables
    public int id;
    public GameObject explosionPrefab;

    public void Initialize(int _id)
    {
        // Set ID
        id = _id;
    }

    public void Explode(Vector3 _position)
    {
        // Take the projectiles position and instantiate an explosion prefab
        transform.position = _position;
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Remove from dictionary and destory projectile
        GameManager.projectiles.Remove(id);
        Destroy(gameObject);
    }
}
