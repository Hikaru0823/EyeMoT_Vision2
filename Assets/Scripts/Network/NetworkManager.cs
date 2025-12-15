using System;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    private INetworkClient _reliableClient;
    private INetworkClient _unreliableClient;

    // Reliable用イベント
    public event Action ReliableConnected;
    public event Action ReliableDisconnected;
    public event Action<string> ReliableMessageReceived;
    public event Action<Exception> ReliableError;

    // Unreliable用イベント
    public event Action UnreliableConnected;
    public event Action UnreliableDisconnected;
    public event Action<string> UnreliableMessageReceived;
    public event Action<Exception> UnreliableError;

    void Awake()
    {
        Instance = this;
    }

    public void InitializeReliable(INetworkClient client)
    {
        if (_reliableClient != null)
        {
            _reliableClient.Connected -= OnReliableConnected;
            _reliableClient.Disconnected -= OnReliableDisconnected;
            _reliableClient.MessageReceived -= OnReliableMessageReceived;
            _reliableClient.Error -= OnReliableError;
        }

        _reliableClient = client;

        if (_reliableClient != null)
        {
            _reliableClient.Connected += OnReliableConnected;
            _reliableClient.Disconnected += OnReliableDisconnected;
            _reliableClient.MessageReceived += OnReliableMessageReceived;
            _reliableClient.Error += OnReliableError;
        }
    }

    public void InitializeUnreliable(INetworkClient client)
    {
        if (_unreliableClient != null)
        {
            _unreliableClient.Connected -= OnUnreliableConnected;
            _unreliableClient.Disconnected -= OnUnreliableDisconnected;
            _unreliableClient.MessageReceived -= OnUnreliableMessageReceived;
            _unreliableClient.Error -= OnUnreliableError;
        }

        _unreliableClient = client;

        if (_unreliableClient != null)
        {
            _unreliableClient.Connected += OnUnreliableConnected;
            _unreliableClient.Disconnected += OnUnreliableDisconnected;
            _unreliableClient.MessageReceived += OnUnreliableMessageReceived;
            _unreliableClient.Error += OnUnreliableError;
        }
    }

    public async Task ConnectReliableAsync()
    {
        if (_reliableClient == null)
            throw new InvalidOperationException("Reliable client not initialized.");

        await _reliableClient.ConnectAsync();
    }

    public async Task ConnectUnreliableAsync()
    {
        if (_unreliableClient == null)
            throw new InvalidOperationException("Unreliable client not initialized.");

        await _unreliableClient.ConnectAsync();
    }

    public void DisconnectAll() 
    {
        DisconnectReliable();
        DisconnectUnreliable();
    }
    public void DisconnectReliable() => _reliableClient?.Disconnect();
    public void DisconnectUnreliable() => _unreliableClient?.Disconnect();

    public void SendReliable(string message)
    {
        _reliableClient?.Send(message);
    }

    public void SendUnreliable(string message)
    {
        _unreliableClient?.Send(message);
    }

    public void Send(Channel ch, string message)
    {
        switch (ch)
        {
            case Channel.Reliable:
                SendReliable(message);
                break;
            case Channel.Unreliable:
                SendUnreliable(message);
                break;
        }
    }

    private void FixedUpdate()
    {
        _reliableClient?.Tick();
        _unreliableClient?.Tick();
    }

    private void OnReliableConnected() => ReliableConnected?.Invoke();
    private void OnReliableDisconnected() => ReliableDisconnected?.Invoke();
    private void OnReliableMessageReceived(string msg) => ReliableMessageReceived?.Invoke(msg);
    private void OnReliableError(Exception ex) => ReliableError?.Invoke(ex);

    private void OnUnreliableConnected() => UnreliableConnected?.Invoke();
    private void OnUnreliableDisconnected() => UnreliableDisconnected?.Invoke();
    private void OnUnreliableMessageReceived(string msg) => UnreliableMessageReceived?.Invoke(msg);
    private void OnUnreliableError(Exception ex) => UnreliableError?.Invoke(ex);

    public enum Channel
    {
        Reliable,
        Unreliable
    }

    public enum NetworkRole
    {
        None,
        Host,
        Client
    }
}

    public class HostInfo
    {
        public string Name;
        public string Address;
        public int TcpPort;
        public int UdpPort;
    }