using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public int id;
    public float hp;
    public float maxHP = 100f;

    //====================================================================
    //                              Functions
    //====================================================================

    public void Initialize(int _id)
    {
        id = _id;
        hp = maxHP;
    }

    // Set the enemy hp using data from the server
    public void SetHP(float _hp)
    {
        hp = _hp;

        if (hp <= 0f)
        {
            GameManager.enemies.Remove(id);
            Destroy(gameObject);
        }
    }
}
