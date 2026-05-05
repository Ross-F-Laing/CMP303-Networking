using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    // Dictionary of all projectiles
    public static Dictionary<int, Projectile> projectiles = new Dictionary<int, Projectile>();
    private static int nextProjectileId = 1;

    public int id;
    public Rigidbody rigidBody;
    public int ownerId;
    public Vector3 initialForce;
    public float explosionRadius = 1.5f;
    public float explosionDamage = 75f;


    //====================================================================
    //                              Functions
    //====================================================================

    public void Initialize(Vector3 _initialMovementDirection, float _initialForceStrength, int _ownerId)
    {
        initialForce = _initialMovementDirection * _initialForceStrength;
        ownerId = _ownerId;
    }

    private void Start()
    {
        // Increment projectile ID and add to dictionary
        id = nextProjectileId;
        nextProjectileId++;
        projectiles.Add(id, this);

        ServerSend.SpawnProjectile(this, ownerId);

        // Add initial force to the projectile
        rigidBody.AddForce(initialForce);
        StartCoroutine(ExplodeAfterTime());
    }

    private void FixedUpdate()
    {
        ServerSend.ProjectilePosition(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Explode()
    {
        ServerSend.ProjectileExploded(this);

        // Create a sphere around the projectile to check for colliders in that region
        Collider[] _colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // Loop through all of the colliders
        foreach (Collider _collider in _colliders)
        {
            // If this collider is a player
            if (_collider.CompareTag("Player"))
            {
                // Deal explosive damage to the collided player
                _collider.GetComponent<Player>().TakeDamage(explosionDamage);
            }

            // If this collider is an enemy
            else if(_collider.CompareTag("Enemy"))
            {
                // Deal explosive damage to the enemy
                _collider.GetComponent<Enemy>().TakeDamage(explosionDamage);
            }
        }

        // Remove projectile from dictionary and destory is from the scene
        projectiles.Remove(id);
        Destroy(gameObject);
    }

    // This function works to serve as a fail safe if the player is to toss an explosive outside the arena or throws it from too high
    private IEnumerator ExplodeAfterTime()
    {
        // Wait for 10 seconds
        yield return new WaitForSeconds(10f);

        // Explode
        Explode();
    }
}
