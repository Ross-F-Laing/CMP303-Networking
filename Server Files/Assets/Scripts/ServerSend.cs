using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    // Send a packet to a client via TCP
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    // Send a packet to a client via UDP
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    // Send a packet to all clients via TCP
    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }

    // Send a packet to all clients except 1 via TCP
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    // Send a packet to all clients via UDP
    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }

    // Send a packet to all clients except 1 via UDP
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    //====================================================================
    //                              Packets
    //====================================================================

    #region Packets

    // Packets are done using the "using" block this is for the packets to be automatically disposed at the end

    //=================================================================================
    //                                      TCP
    //=================================================================================

    // TCP is used for information that is vital, it will always arrive accurately, just slower than UDP
    // Any packets that are vital to have correct information will use TCP


    // Send a welcome message to the given client
    public static void Welcome(int _toClient, string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    // Tell a given client to spawn a player
    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            SendTCPData(_toClient, _packet);
        }
    }

    // Disconnect a player and send that data to all other clients
    public static void PlayerDisconnected(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);

            SendTCPDataToAll(_packet);
        }
    }

    // Write a specific player's health and id and send it to all clients
    public static void PlayerHealth(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.health);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerRespawned(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawned))
        {
            _packet.Write(_player.id);

            SendTCPDataToAll(_packet);
        }
    }

    public static void CreateItemSpawner(int _toClient, int _spawnerId, Vector3 _spawnerPosition, bool _hasItem)
    {
        using (Packet _packet = new Packet((int)ServerPackets.createItemSpawner))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_spawnerPosition);
            _packet.Write(_hasItem);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void ItemSpawned(int _spawnerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemSpawned))
        {
            _packet.Write(_spawnerId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ItemPickedUp(int _spawnerId, int _byPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemPickedUp))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnProjectile(Projectile _projectile, int _thrownByPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnProjectile))
        {
            // Write projectile ID, position, and the player it was thrown by and send it to the packet
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);
            _packet.Write(_thrownByPlayer);

            // Send the packet through TCP
            SendTCPDataToAll(_packet);
        }
    }

    public static void ProjectileExploded(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectileExploded))
        {
            // Write projectile ID and position and send it to the packet
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            // Send the packet through TCP
            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnEnemy(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnEnemy))
        {
            SendTCPDataToAll(SpawnEnemy_Data(_enemy, _packet));
        }
    }

    public static void SpawnEnemy(int _toClient, Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnEnemy))
        {
            SendTCPDataToAll(_toClient, SpawnEnemy_Data(_enemy, _packet));
        }
    }

    private static Packet SpawnEnemy_Data(Enemy _enemy, Packet _packet)
    {
        _packet.Write(_enemy.id);
        _packet.Write(_enemy.transform.position);
        return _packet;
    }

    public static void EnemyHP(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyHP))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.hp);

            SendTCPDataToAll(_packet);
        }
    }

    //=================================================================================
    //                                      UDP
    //=================================================================================

    // UDP is faster than TCP but is less reliable
    // Packets that handle information for smoothnes will be handled by UDP

    // Send a player's updated position to all clients
    public static void PlayerPosition(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    // Send a player's updated rotation to all the clients except for themself
    public static void PlayerRotation(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.rotation);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    public static void ProjectilePosition(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectilePosition))
        {
            // Write projectile ID and position and send it to the packet
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            // Send the packet through TCP
            SendUDPDataToAll(_packet);
        }
    }

    public static void EnemyPosition(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyPosition))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    #endregion
}
