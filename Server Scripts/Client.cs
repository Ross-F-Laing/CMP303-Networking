using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

public class Client
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public static int dataBufferSize = 4096;    // Data buffer size of 4MB

    public int id;
    public Player player;
    public TCP tcp;
    public UDP udp;

    //====================================================================
    //                              Classes
    //====================================================================

    public Client(int _clientId)
    {
        // Assign client ID and initialize TCP and UDP instances
        id = _clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    public class TCP
    {
        //====================================================================
        //                         TCP Global Variables
        //====================================================================

        public TcpClient socket;

        private readonly int id;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        //====================================================================
        //                            TCP Functions
        //====================================================================

        public TCP(int _id)
        {
            id = _id;   // Set ID on construct
        }

        // Initializes new connected clients TCP info
        public void Connect(TcpClient _socket)
        {
            // Assign TCP client to the socket field
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            stream = socket.GetStream();

            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            // Begin reading data on the stream
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            ServerSend.Welcome(id, "Welcome to the server!");
        }

        // Send data to client using TCP
        public void SendData(Packet _packet)
        {
            try
            {
                // If the socket is valid
                if (socket != null)
                {
                    // Send data to appropriate client
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {_ex}");
            }
        }

        // Read incoming data from stream
        private void ReceiveCallback(IAsyncResult _result)
        {
            // Try catch is used to avoid potential server crashes
            try
            {
                int _byteLength = stream.EndRead(_result);  // EndRead returns the number of bytes read to the stream

                // If there is no read data
                if (_byteLength <= 0)
                {
                    // Disconnect the client
                    Server.clients[id].Disconnect();
                    return;
                }

                // Copy new data into a new array
                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                // If data receieved was already handled reset it
                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                // Debug log error message and disconnect client
                Debug.Log($"Error receiving TCP data: {_ex}");
                Server.clients[id].Disconnect();
            }
        }

        // Prepare received data to be used by appropriate packet handler methods
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            // Set receievedData to what was just receieved from stream
            receivedData.SetBytes(_data);

            // If receivedData has at least 4 unread bytes (This means there is still a packet remaining
            if (receivedData.UnreadLength() >= 4)
            {
                // Read the lenght of the packet
                _packetLength = receivedData.ReadInt();

                // If the packet contains no data
                if (_packetLength <= 0)
                {
                    // Reset receivedData instance to allow it to be reused
                    return true;
                }
            }


            // While packets contains data and the packet lenght doesn't exceed the lenght of the packet we're reading
            // (This means receivedData has another complete packet to be handled)
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // Read packet bytes into a new byte array
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);

                // Ensure this is executed on main thread
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    // Create a new packet
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();

                        // Call appropriate method to handle the packet
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });

                // Reset packet length
                _packetLength = 0;

                // If client's received data contains another packet
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();

                    // If packet has no data reset receievedData so it can be reused
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            // reset receievedData so it can be reused
            if (_packetLength <= 1)
            {
                return true;
            }

            // _packetLength is greater than 1 so there is still a partial packet in here
            return false;
        }

        // Close the TCP connection
        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        //====================================================================
        //                        UDP Global Variables
        //====================================================================

        public IPEndPoint endPoint;

        private int id;

        //====================================================================
        //                            UDP Functions
        //====================================================================

        public UDP(int _id)
        {
            id = _id;
        }

        // Initialize the new client's UDP related info
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        // Send data to the client via UDP
        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        // Prepare received data to be used by appropriate packer handler methods
        public void HandleData(Packet _packetData)
        {
            int _packetLength = _packetData.ReadInt();
            byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    Server.packetHandlers[_packetId](id, _packet); // Call appropriate method to handle the packet
                }
            });
        }

        // Clean up UDP connection
        public void Disconnect()
        {
            endPoint = null;
        }
    }

    //====================================================================
    //                       Global Functions
    //====================================================================

    // Send client into the game let other clients know of the new client
    public void SendIntoGame(string _playerName)
    {
        player = NetworkManager.instance.InstantiatePlayer();
        player.Initialize(id, _playerName);

        // Send all players to the new player
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.SpawnPlayer(id, _client.player);
                }
            }
        }

        // Send the new player to all players (including themself)
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        foreach (ItemSpawner _itemSpawner in ItemSpawner.spawners.Values)
        {
            ServerSend.CreateItemSpawner(id, _itemSpawner.spawnerId, _itemSpawner.transform.position, _itemSpawner.hasItem);
        }

        foreach(Enemy _enemy in Enemy.enemies.Values)
        {
            ServerSend.SpawnEnemy(id, _enemy);
        }
    }

    // Disconnect the client and stop all traffic
    private void Disconnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;
        });

        tcp.Disconnect();
        udp.Disconnect();

        ServerSend.PlayerDisconnected(id);
    }
}
