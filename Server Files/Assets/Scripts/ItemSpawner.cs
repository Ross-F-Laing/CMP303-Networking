using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public static Dictionary<int, ItemSpawner> spawners = new Dictionary<int, ItemSpawner>();
    private static int nextSpawnerId = 1;

    public int spawnerId;
    public bool hasItem = false;

    //====================================================================
    //                              Functions
    //====================================================================

    private void Start()
    {
        hasItem = false;
        spawnerId = nextSpawnerId;
        nextSpawnerId++;
        spawners.Add(spawnerId, this);

        StartCoroutine(SpawnItem());
    }

    // If player collides with item call for a pick up
    private void OnTriggerEnter(Collider other)
    {
        if (hasItem && other.CompareTag("Player"))
        {
            Player _player = other.GetComponent<Player>();
            if (_player.AttemptPickupItem())
            {
                // If player successfully picked up item, send data to server
                ItemPickedUp(_player.id);
            }
        }
    }

    // Spawn an item into the scene
    private IEnumerator SpawnItem()
    {
        yield return new WaitForSeconds(10f);

        hasItem = true;
        ServerSend.ItemSpawned(spawnerId);
    }

    // Send pick up info to client
    private void ItemPickedUp(int _byPlayer)
    {
        hasItem = false;
        ServerSend.ItemPickedUp(spawnerId, _byPlayer);

        StartCoroutine(SpawnItem());
    }
}
