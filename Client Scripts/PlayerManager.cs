using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public int id;
    public string username;
    public float health;
    public float maxHealth = 100f;
    public int itemCount = 0;
    public MeshRenderer model;

    //====================================================================
    //                              Functions
    //====================================================================

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
    }

    public void SetHealth(float _health)
    {
        // Set health to incoming value
        health = _health;

        // If dead
        if (health <= 0f)
        {
            // Kill player
            Die();
        }
    }

    public void Die()
    {
        // Disable model
        model.enabled = false;
    }

    public void Respawn()
    {
        // Re-enable model and reset health to max health
        model.enabled = true;
        SetHealth(maxHealth);
    }
}
