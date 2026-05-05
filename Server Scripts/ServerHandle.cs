using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ServerHandle
{
    static float nowSecond = 0;
    static float nowMillisecond = 0;

    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        // Read data
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        
        // Check for correct and valid client ID
        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
    }

    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        bool[] _inputs = new bool[_packet.ReadInt()];
        for (int i = 0; i < _inputs.Length; i++)
        {
            _inputs[i] = _packet.ReadBool();
        }
        float newSecond = _packet.ReadFloat();
        float newMillisecond = _packet.ReadFloat();
        Quaternion _rotation = _packet.ReadQuaternion();

        if(newSecond >= nowSecond || (nowSecond > 35 && newSecond < 30))
        {
            if(newMillisecond > nowMillisecond || (nowMillisecond > 800 && newMillisecond < 400)) 
            {
                Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
            }
        }
    }

    public static void PlayerShoot(int _fromClient, Packet _packet)
    {
        // Read the packet for the direction the shot should go6
        Vector3 _shootDirection = _packet.ReadVector3();

        // Call the players shoot function using that direction
        Server.clients[_fromClient].player.Shoot(_shootDirection);
    }

    public static void PlayerThrowItem(int _fromClient, Packet _packet)
    {
        // Read the packet for the direction the player will throw the item
        Vector3 _throwDirection = _packet.ReadVector3();

        // Call the players throw item function using that direciton
        Server.clients[_fromClient].player.ThrowItem(_throwDirection);
    }
}
