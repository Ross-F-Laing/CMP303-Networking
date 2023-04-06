using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// State machine enum
public enum EnemyState
{
    idle,
    patrol,
    chase,
    attack
}

public class Enemy : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public static int maxEnemies = 10;
    public static Dictionary<int, Enemy> enemies = new Dictionary<int, Enemy>();
    private static int nextEnemyId = 1;

    public int id;
    public EnemyState state;
    public Player target;
    public CharacterController controller;
    public Transform shootOrigin;
    public float gravity = -9.81f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float hp;
    public float maxhp = 100f;
    public float detectionRange = 10f;
    public float shotRange = 5f;
    public float shotAccuracy = 0.1f;
    public float patrolDuration = 3f;
    public float idleDuration = 1f;
    private float shotCooldown = 5f;

    private bool isPatrolRoutineRunning;
    private float yVelocity = 0;

    //====================================================================
    //                              Functions
    //====================================================================

    private void Start()
    {
        id = nextEnemyId;
        nextEnemyId++;
        hp = maxhp;

        enemies.Add(id, this);

        ServerSend.SpawnEnemy(this);

        state = EnemyState.patrol;
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        patrolSpeed *= Time.fixedDeltaTime;
        chaseSpeed *= Time.fixedDeltaTime;
    }

    // State machine
    private void FixedUpdate()
    {
        switch (state)
        {
            case EnemyState.idle:
                LookForPlayer();
                break;
            case EnemyState.patrol:
                if (!LookForPlayer())
                {
                    Patrol();
                }
                break;
            case EnemyState.chase:
                Chase();
                break;
            case EnemyState.attack:
                Attack();
                break;
            default:
                break;
        }
    }

    // Enemy will search for player
    private bool LookForPlayer()
    {
        // Loop through all clients
        foreach (Client _client in Server.clients.Values)
        {
            // If the client is not a player (so is an enemy)
            if (_client.player != null)
            {
                // Create a vector to the player
                Vector3 _enemyToPlayer = _client.player.transform.position - transform.position;

                // If player is withing detection range
                if (_enemyToPlayer.magnitude <= detectionRange)
                {
                    // Ray cast towards player
                    if (Physics.Raycast(shootOrigin.position, _enemyToPlayer, out RaycastHit _hit, detectionRange))
                    {
                        // If nearest object is a player
                        if (_hit.collider.CompareTag("Player"))
                        {
                            // Stop patrolling and start chasing the player
                            target = _hit.collider.GetComponent<Player>();
                            if (isPatrolRoutineRunning)
                            {
                                isPatrolRoutineRunning = false;
                                StopCoroutine(StartPatrol());
                            }

                            state = EnemyState.chase;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // Enemy will randomly patrol the map
    private void Patrol()
    {
        if (!isPatrolRoutineRunning)
        {
            StartCoroutine(StartPatrol());
        }

        Move(transform.forward, patrolSpeed);
    }

    // Enemy will start patrol
    private IEnumerator StartPatrol()
    {
        // Pick a random spot nearby and move towards it
        isPatrolRoutineRunning = true;
        Vector2 _randomPatrolDirection = Random.insideUnitCircle.normalized;
        transform.forward = new Vector3(_randomPatrolDirection.x, 0f, _randomPatrolDirection.y);

        // Briefly pause before finding new patrol points
        yield return new WaitForSeconds(patrolDuration);

        state = EnemyState.idle;

        yield return new WaitForSeconds(idleDuration);

        state = EnemyState.patrol;
        isPatrolRoutineRunning = false;
    }

    // Enemy will chase the player
    private void Chase()
    {
        if (CanSeeTarget())
        {
            // Create a vector from enemy to player
            Vector3 _enemyToPlayer = target.transform.position - transform.position;
            
            // If target is withing shooting range attack target
            if (_enemyToPlayer.magnitude <= shotRange)
            {
                state = EnemyState.attack;
            }
            // Otherwise move closer to target
            else
            {
                Move(_enemyToPlayer, chaseSpeed);
            }
        }
        else
        {
            target = null;
            state = EnemyState.patrol;
        }
    }

    // Enemy attacks the player
    private void Attack()
    {
        if (CanSeeTarget())
        {
            // Create a vector to the target and move towards it
            Vector3 _enemyToPlayer = target.transform.position - transform.position;
            transform.forward = new Vector3(_enemyToPlayer.x, 0f, _enemyToPlayer.z);

            // If target it within range shoot player
            if (_enemyToPlayer.magnitude <= shotRange)
            {
                Shoot(_enemyToPlayer);
            }
            // If target not in range move towards target
            else
            {
                Move(_enemyToPlayer, chaseSpeed);
            }
        }
        else
        {
            target = null;
            state = EnemyState.patrol;
        }
    }

    // Move the enemy
    private void Move(Vector3 _direction, float _speed)
    {
        // Calculate movement vector
        _direction.y = 0f;
        transform.forward = _direction;
        Vector3 _movement = transform.forward * _speed;

        // If enemy is on ground clamp vertical velocity to 0 otherwise apply gravity
        if (controller.isGrounded)
        {
            yVelocity = 0f;
        }
        yVelocity += gravity;

        // Apply movement to enemy
        _movement.y = yVelocity;
        controller.Move(_movement);

        ServerSend.EnemyPosition(this);
    }

    // Shoot at the player
    private void Shoot(Vector3 _shootDirection)
    {
        // If 5 second cooldown is over
        if(shotCooldown <= 0)
        {
            if (Physics.Raycast(shootOrigin.position, _shootDirection, out RaycastHit _hit, shotRange))
            {
                // If enemy has shot player
                if (_hit.collider.CompareTag("Player"))
                {
                    // Check for accuracy
                    if (Random.Range(0, 10) <= shotAccuracy)
                    {
                        // Deal damage to the player
                        _hit.collider.GetComponent<Player>().TakeDamage(20f);
                    }
                }
            }
        }
        // If 5 second cooldown isn't over, decrease cooldown
        else
        {
            shotCooldown -= Time.deltaTime;
        }
    }

    // Enemy takes damage
    public void TakeDamage(float _damage)
    {
        // Decrease hp by damage
        hp -= _damage;

        // If dead
        if (hp <= 0f)
        {
            // Set hp to 0 and remove enemy
            hp = 0f;

            enemies.Remove(id);
            Destroy(gameObject);
        }

        // Send HP update
        ServerSend.EnemyHP(this);
    }

    // Bool check for if the enemy can see a target player
    private bool CanSeeTarget()
    {
        if (target == null)
        {
            return false;
        }

        if (Physics.Raycast(shootOrigin.position, target.transform.position - transform.position, out RaycastHit _hit, detectionRange))
        {
            // If the object infront of the enemy is a player return true
            if (_hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }
}
