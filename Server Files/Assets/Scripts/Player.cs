using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public int id;
    public string username;
    public CharacterController controller;
    public Transform shootOrigin;
    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;
    public float throwForce = 600f;
    public float health;
    public float maxHealth = 100f;
    public int itemAmount = 0;
    public int maxItemAmount = 3;
    public float gracePeriodDefault = 5f;
    public float gracePeriod;

    private bool[] inputs;
    private float yVelocity = 0;

    //====================================================================
    //                              Functions
    //====================================================================

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
        gracePeriod = gracePeriodDefault;
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;

        inputs = new bool[5];
    }

    // Process player input and move the player
    public void FixedUpdate()
    {
        // If player is dead don't update
        if (health <= 0f)
        {
            return;
        }

        Vector2 _inputDirection = Vector2.zero;
        if (inputs[0])
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])
        {
            _inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            _inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            _inputDirection.x += 1;
        }

        Move(_inputDirection);

        if(gracePeriod > 0f) { gracePeriod -=  Time.fixedDeltaTime; }
    }

    // Calculate where player wants to move to and move player in that direction
    private void Move(Vector2 _inputDirection)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;

        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }
        yVelocity += gravity;

        _moveDirection.y = yVelocity;
        controller.Move(_moveDirection);

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);        
    }

    // Update player input with received input
    public void SetInput(bool[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }

    public void Shoot(Vector3 _viewDirection)
    {
        // If dead don't shoot
        if (health <= 0f)
        {
            return;
        }

        // Cast a ray from player's eyeline 25 units forward
        if (Physics.Raycast(shootOrigin.position, _viewDirection, out RaycastHit _hit, 25f))
        {
            // If collider is a player
            if (_hit.collider.CompareTag("Player"))
            {
                // Deal damage to collided player
                _hit.collider.GetComponent<Player>().TakeDamage(50f);
            }

            // If collider is an enemy
            else if (_hit.collider.CompareTag("Enemy"))
            {
                // Deal damage to the enemy
                _hit.collider.GetComponent<Enemy>().TakeDamage(50f);
            }
        }
    }

    public void ThrowItem(Vector3 _viewDirection)
    {
        // If dead don't throw
        if (health <= 0f)
        {
            return;
        }

        // If the player has an item
        if (itemAmount > 0)
        {
            // remove item from player
            itemAmount--;

            // Instantiate projectile
            NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(_viewDirection, throwForce, id);
        }
    }

    public void TakeDamage(float _damage)
    {
        // If dead, can't take damage
        if (health <= 0f)
        {
            return;
        }

        if(gracePeriod < 0f)
        {
            // Remove damage from health
            health -= _damage;

            // If player is now dead
            if (health <= 0f)
            {
                // Reset health to 0 to not have negative HP
                health = 0f;

                // Disable and reset the player
                controller.enabled = false;
                transform.position = new Vector3(0f, 25f, 0f);
                ServerSend.PlayerPosition(this);
                StartCoroutine(Respawn());
            }

            gracePeriod = 5f;
        }

        

        ServerSend.PlayerHealth(this);
    }

    // Respawn the player
    private IEnumerator Respawn()
    {
        // Delay by 5 seconds
        yield return new WaitForSeconds(5f);

        // Reset player HP
        health = maxHealth;

        // Re-enable character controller
        controller.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

    // If there is an item pick it up
    public bool AttemptPickupItem()
    {
        // If player already has a full inventory don't pick up
        if (itemAmount >= maxItemAmount)
        {
            return false;
        }

        // Pick up item
        itemAmount++;
        return true;
    }
}
