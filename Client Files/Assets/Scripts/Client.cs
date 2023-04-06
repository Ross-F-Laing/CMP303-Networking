using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public static Client instance;
    public static int dataBufferSize = 4096;    // Data buffer size of 4MB

    public string ip = "127.0.0.1";             // IP for local host
    public int port = 26950;                    // Port for this game server
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private bool isConnected = false;
    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    //====================================================================
    //                              Functions
    //====================================================================

    // Create 1 single instance of this class
    private void Awake()
    {
        // If there is isn't an instance
        if (instance == null)
        {
            // Set instance to this class
            instance = this;
        }
        // If there is an instance and it's not this class
        else if (instance != this)
        {
            // Debug log message and destroy this class
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    private void OnApplicationQuit()
    {
        Disconnect(); // Disconnect when the game is closed
    }

    // Connect to the server
    public void ConnectToServer()
    {
        InitializeClientData();

        isConnected = true;
        tcp.Connect(); // Connect tcp, udp gets connected once tcp is done
    }

    //====================================================================
    //                               TCP
    //====================================================================

    public class TCP
    {
        //====================================================================
        //                        TCP Global Variables
        //====================================================================

        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        //====================================================================
        //                            TCP Functions
        //====================================================================

        // Connect to the server using TCP
        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            // Initialize receive buffer and begin connection to the socket
            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        // Initialize the client with it's TCP info
        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            // Exit if not connected
            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        // Send data to the client via TCP
        public void SendData(Packet _packet)
        {
            try
            {
                // If there is no socket
                if (socket != null)
                {
                    // Send data to server
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
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
                    // Disconnect the instance
                    instance.Disconnect();
                    return;
                }

                // Copy new data into a new array
                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                // If data receieved was already handled reset it
                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                // Disconnect client due to error receiving data
                Disconnect();
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
                        // Read packet ID and use that to call an appropriate method to handle the packet
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
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

        // Disconnect from the server and clean up TCP connection
        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    //====================================================================
    //                                UDP
    //====================================================================

    public class UDP
    {
        //====================================================================
        //                        UDP Global Variables
        //====================================================================

        public UdpClient socket;
        public IPEndPoint endPoint;

        //====================================================================
        //                            UDP Functions
        //====================================================================

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        // Connect to the server via UDP
        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        // Send data to the client via UDP
        public void SendData(Packet _packet)
        {
            try
            {
                _packet.InsertInt(instance.myId); // Insert the client's ID at the start of the packet
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        // Receive incoming UDP data
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (_data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                HandleData(_data);
            }
            catch
            {
                Disconnect();
            }
        }

        // Prepare received data to be used by the appropriate packet handler method
        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    packetHandlers[_packetId](_packet); // Call appropriate method to handle the packet
                }
            });
        }

        // Disconnect from server and clean up UDP
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    // Initialize all necessary client data
    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation },
            { (int)ServerPackets.playerDisconnected, ClientHandle.PlayerDisconnected },
            { (int)ServerPackets.playerHealth, ClientHandle.PlayerHealth },
            { (int)ServerPackets.playerRespawned, ClientHandle.PlayerRespawned },
            { (int)ServerPackets.createItemSpawner, ClientHandle.CreateItemSpawner },
            { (int)ServerPackets.itemSpawned, ClientHandle.ItemSpawned },
            { (int)ServerPackets.itemPickedUp, ClientHandle.ItemPickedUp },
            { (int)ServerPackets.spawnProjectile, ClientHandle.SpawnProjectile },
            { (int)ServerPackets.projectilePosition, ClientHandle.ProjectilePosition },
            { (int)ServerPackets.projectileExploded, ClientHandle.ProjectileExploded },
            { (int)ServerPackets.spawnEnemy, ClientHandle.SpawnEnemy },
            { (int)ServerPackets.enemyPosition, ClientHandle.EnemyPosition },
            { (int)ServerPackets.enemyHP, ClientHandle.EnemyHP }
        };
        Debug.Log("Initialized packets.");
    }

    // Disconnect from the server and stop all network traffic
    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected from server.");
        }
    }
}
