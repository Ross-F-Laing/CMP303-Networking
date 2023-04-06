using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{

    // All packet reads must be in the same order they were written to ensure no errors

    //====================================================================
    //                              Functions
    //====================================================================

    public static void Welcome(Packet _packet)
    {
        // Read variables
        string _msg = _packet.ReadString();
        int _myId = _packet.ReadInt();

        Debug.Log($"Message from server: {_msg}");

        // Set the client's ID and send a received packet
        Client.instance.myId = _myId;
        ClientSend.WelcomeReceived();

        // Now that we have the client's id, connect UDP
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void SpawnPlayer(Packet _packet)
    {
        int _id = _packet.ReadInt();
        string _username = _packet.ReadString();
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();

        GameManager.instance.SpawnPlayer(_id, _username, _position, _rotation);
    }

    public static void PlayerPosition(Packet _packet)
    {
        int _id = _packet.ReadInt();

        Vector3 _position = _packet.ReadVector3();

        // UDP sends data faster than TCP so position could be sent faster than the player existing
        // To avoid this error a TryGetValue is used to see if the player exists yet
        if(GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            GameManager.players[_id].transform.position = _position;
        }
        
    }

    public static void PlayerRotation(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Quaternion _rotation = _packet.ReadQuaternion();

        // UDP sends data faster than TCP so rotation could be sent faster than the player existing
        // To avoid this error a TryGetValue is used to see if the player exists yet
        if (GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            GameManager.players[_id].transform.rotation = _rotation;
        }
    }

    public static void PlayerDisconnected(Packet _packet)
    {
        int _id = _packet.ReadInt();

        Destroy(GameManager.players[_id].gameObject);
        GameManager.players.Remove(_id);
    }

    public static void PlayerHealth(Packet _packet)
    {
        int _id = _packet.ReadInt();
        float _health = _packet.ReadFloat();

        GameManager.players[_id].SetHealth(_health);
    }

    public static void PlayerRespawned(Packet _packet)
    {
        int _id = _packet.ReadInt();

        GameManager.players[_id].Respawn();
    }

    public static void CreateItemSpawner(Packet _packet)
    {
        int _spawnerId = _packet.ReadInt();
        Vector3 _spawnerPosition = _packet.ReadVector3();
        bool _hasItem = _packet.ReadBool();

        GameManager.instance.CreateItemSpawner(_spawnerId, _spawnerPosition, _hasItem);
    }

    public static void ItemSpawned(Packet _packet)
    {
        int _spawnerId = _packet.ReadInt();

        GameManager.itemSpawners[_spawnerId].ItemSpawned();
    }

    public static void ItemPickedUp(Packet _packet)
    {
        int _spawnerId = _packet.ReadInt();
        int _byPlayer = _packet.ReadInt();

        GameManager.itemSpawners[_spawnerId].ItemPickedUp();
        GameManager.players[_byPlayer].itemCount++;
    }

    public static void SpawnProjectile(Packet _packet)
    {
        // Read variables
        int _projectileId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();
        int _thrownByPlayer = _packet.ReadInt();

        // Spawn projectile and decrement their item count
        GameManager.instance.SpawnProjectile(_projectileId, _position);
        GameManager.players[_thrownByPlayer].itemCount--;
    }

    public static void ProjectilePosition(Packet _packet)
    {
        // Read variables
        int _projectileId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        // Set projectile position
        GameManager.projectiles[_projectileId].transform.position = _position;
    }

    public static void ProjectileExploded(Packet _packet)
    {
        // Read variables
        int _projectileId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        // Call explode function
        GameManager.projectiles[_projectileId].Explode(_position);
    }

    public static void SpawnEnemy(Packet _packet)
    {
        int _enemyId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        GameManager.instance.SpawnEnemy(_enemyId, _position);
    }

    public static void EnemyPosition(Packet _packet)
    {
        int _enemyId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        // TryGetValue is used to see if enemy exists yet, UDP is faster than TCP so this protects agaisnt that
        if (GameManager.enemies.TryGetValue(_enemyId, out EnemyManager _enemy))
        {
            _enemy.transform.position = _position;
        }
    }

    public static void EnemyHP(Packet _packet)
    {
        int _enemyId = _packet.ReadInt();
        float _hp = _packet.ReadFloat();

        GameManager.enemies[_enemyId].SetHP(_hp);
    }
}
