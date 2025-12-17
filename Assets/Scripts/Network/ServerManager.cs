using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    public Dictionary<int, ClientSession> Clients = new Dictionary<int, ClientSession>();

    public event Action<TcpServer.ClientConnection> ClientConnected;
    public event Action<TcpServer.ClientConnection> ClientDisconnected;
    public event Action<IPEndPoint, string> TcpMessageReceived;
    public event Action<Exception> TcpError;
    public event Action<IPEndPoint, string> UdpMessageReceived;
    public event Action<Exception> UdpError;

    private IServer _tcpServer;
    private IServer _udpServer;
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

    public void EnqueueMainThread(Action action)
    {
        if (action == null) return;
        _mainThreadQueue.Enqueue(action);
    }

    public void AddListener(IServerCallbacks server)
    {
        ClientConnected += server.OnClientConnected;
        ClientDisconnected += server.OnClientDisconnected;
        TcpMessageReceived += server.OnMessageReceived;
        TcpError += server.OnTcpError;
        UdpMessageReceived += server.OnMessageReceived;
        UdpError += server.OnUdpError;
    }

    public void RemoveListener(IServerCallbacks server)
    {
        ClientConnected -= server.OnClientConnected;
        ClientDisconnected -= server.OnClientDisconnected;
        TcpMessageReceived -= server.OnMessageReceived;
        TcpError -= server.OnTcpError;
        UdpMessageReceived -= server.OnMessageReceived;
        UdpError -= server.OnUdpError;
    }

    public void RemoveListener(IServer server)
    {
        if (server is TcpServer tcpServer)
        {
            tcpServer.ClientConnected -= OnTcpClientConnected;
            tcpServer.ClientDisconnected -= OnTcpClientDisconnected;
            server.MessageReceived -= OnTcpMessageReceived;
            server.Error -= OnTcpError;
        }
        else if (server is UdpServer)
        {
            server.MessageReceived -= OnUdpMessageReceived;
            server.Error -= OnUdpError;
        }
    }

    public void InitializeTcp(IServer server)
    {
        if (_tcpServer != null)
        {
            (_tcpServer as TcpServer).ClientConnected -= OnTcpClientConnected;
            (_tcpServer as TcpServer).ClientDisconnected -= OnTcpClientDisconnected;
            _tcpServer.MessageReceived -= OnTcpMessageReceived;
            _tcpServer.Error -= OnTcpError;
        }

        _tcpServer = server;

        if (_tcpServer  != null)
        {
            (_tcpServer as TcpServer).ClientConnected += OnTcpClientConnected;
            (_tcpServer as TcpServer).ClientDisconnected += OnTcpClientDisconnected;
            _tcpServer.MessageReceived += OnTcpMessageReceived;
            _tcpServer.Error += OnTcpError;
        }
    }

    public void InitializeUdp(IServer server)
    {
        if (_udpServer != null)
        {
            _udpServer.MessageReceived -= OnUdpMessageReceived;
            _udpServer.Error -= OnUdpError;
        }

        _udpServer = server;

        if (_udpServer != null)
        {
            _udpServer.MessageReceived += OnUdpMessageReceived;
            _udpServer.Error += OnUdpError;
        }
    }

    public void StartTcp()
    {
        if (_tcpServer == null)
        {
            Debug.LogError("TCP Server is not initialized.");
            return;
        }
        _tcpServer.StartServer();
    }

    public void StartUdp()
    {
        if (_udpServer == null)
        {
            Debug.LogError("UDP Server is not initialized.");
            return;
        }
        _udpServer.StartServer();
    }

    public void Stop()
    {
        Instance = null;
        _tcpServer?.Stop();
        _udpServer?.Stop();
    }

    public void OnTcpClientConnected(TcpServer.ClientConnection client)
    {
        if(!Clients.ContainsKey(client.Id))
        {
            Clients.Add(client.Id, new ClientSession
            {
                Id = client.Id,
                Tcp = client,
                Udp = null
            });
        }
        EnqueueMainThread(() => ClientConnected?.Invoke(client));
    }

    public void OnTcpClientDisconnected(TcpServer.ClientConnection client)
    {
        EnqueueMainThread(() => ClientDisconnected?.Invoke(client));
        if(Clients.ContainsKey(client.Id))
        {
            Clients.Remove(client.Id);
        }
    }

    public async void OnTcpMessageReceived(IPEndPoint sender, string message)
    {
        EnqueueMainThread(() => TcpMessageReceived?.Invoke(sender, message));
        await SendMessage(_tcpServer, message, sender);
    }

    public async void OnUdpMessageReceived(IPEndPoint sender, string message)
    {
        EnqueueMainThread(() => UdpMessageReceived?.Invoke(sender, message));
        await SendMessage(_udpServer, message, sender);
    }

    public async void SendTcp(string message)
    {
        await SendMessage(_tcpServer,message);
    }

    public async Task SendMessage(IServer server, string message, IPEndPoint sender = null)
    {
        var header = NetJson.FromJson<NetMessage<object>>(message);
        switch (header.TargetId)
        {
            case 0:
                List<ClientSession> snapshot;
                lock (Clients)
                {
                    snapshot = new List<ClientSession>(Clients.Values);
                }
                await server.BroadcastAsync(snapshot, message);
                break;
            case -1:
                if(header.Type == NetMessageType.UdpConnectRequest && sender != null)
                {
                    ClientSession udpSession;
                    lock(Clients)
                    {
                        if(Clients.TryGetValue(header.SenderId, out udpSession))
                        {
                            udpSession.Udp = sender;
                        }
                        else
                        {
                            Debug.LogWarning($"[Server] Client {header.SenderId} not found for UDP connect.");
                            return;
                        }
                    }
                    Debug.Log($"[Server] Client {header.SenderId} UDP connected from {sender}.");
                }
                break;
            default:
                ClientSession session;
                lock(Clients)
                {
                    if(!Clients.TryGetValue(header.TargetId, out session))
                    {
                        Debug.LogWarning($"[Server] Target client {header.TargetId} not found or has TCP connection.");
                        return;
                    }
                }
                await server.SendToClientAsync(session, message);
                break;
        }
    }

    public void OnTcpError(Exception ex)
    {
        EnqueueMainThread(() => TcpError?.Invoke(ex));
    }

    public void OnUdpError(Exception ex)
    {
        EnqueueMainThread(() => UdpError?.Invoke(ex));
    }

    void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}

public class ClientSession
{
    public int Id;
    public TcpServer.ClientConnection Tcp;   // null あり
    public System.Net.IPEndPoint Udp; // null あり
}