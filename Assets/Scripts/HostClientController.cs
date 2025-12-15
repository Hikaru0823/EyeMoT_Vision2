using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class HostClientController : MonoBehaviour
{
    [Header("State")]
    [SerializeField, ReadOnly] private NetworkManager.NetworkRole _currentRole = NetworkManager.NetworkRole.None;

    [Header("Resources")]
    [SerializeField] private int maxClients = 4;
    [SerializeField] private int _tcpPort = 8080;
    [SerializeField] private int _udpPort = 8081;
    [SerializeField] private int _udpDiscoveryPort = 53000;
    [SerializeField] private ClientMouseController _clientMousePrefab;
    [SerializeField] private GameObject _viewPanel;
    public event Action ConnectedToServer;
    private Dictionary<int, ClientMouseController> _clientMouseControllers = new();
    private TcpServer _tcpServer;
    private UdpServer _udpServer;

    private void OnDestroy()
    {
        CleanupCurrentRole();
    }

    private void OnApplicationQuit()
    {
        CleanupCurrentRole();
    }

    public async void StartHost()
    {
        Debug.Log("Switch role to Host");

        CleanupCurrentRole();

        // --- サーバ起動（TCP + UDP） ---
        _tcpServer = new TcpServer(_tcpPort);
        _tcpServer.ClientConnected += async id => await OnClientConnected(id);
        _tcpServer.ClientDisconnected += id => OnClientDisconnected(id);
        _tcpServer.MessageReceived += (id, msg) => OnMessageReceived(id, msg);
        _tcpServer.Error += ex => Debug.LogError("[TCP Server] " + ex);
        _tcpServer.StartServer();

        _udpServer = new UdpServer(_udpPort, _udpDiscoveryPort);
        _udpServer.ClientRegistered += id => Debug.Log($"[UDP Server] Client {id} registered");
        _udpServer.MessageReceived += (id, msg) => OnMessageReceived(id, msg);
        _udpServer.Error += ex => Debug.LogError("[UDP Server] " + ex);
        _udpServer.StartServer();

        Debug.Log($"Servers started: TCP:{_tcpPort}, UDP:{_udpPort}");

        // 自分もクライアントとして localhost に接続
        //await StartClientsAsync("127.0.0.1");

        _currentRole = NetworkManager.NetworkRole.Host;
    }

    public async void StartClient()
    {
        Debug.Log("Switch role to Client");

        CleanupCurrentRole();

        var hostIp = "127.0.0.1";

        await StartClientsAsync(hostIp);

        _currentRole = NetworkManager.NetworkRole.Client;
        ConnectedToServer?.Invoke();
    }

    public void Disconnect()
    {
        Debug.Log("Disconnect / stop host");
        CleanupCurrentRole();
        _currentRole = NetworkManager.NetworkRole.None;
    }

    private async Task StartClientsAsync(string hostIp)
    {
        var reliable = new TcpNetworkClient(hostIp, _tcpPort);
        var unreliable = new UdpNetworkClient(hostIp, _udpPort);

        var nm = NetworkManager.Instance;
        nm.InitializeReliable(reliable);
        nm.InitializeUnreliable(unreliable);

        nm.ReliableConnected += OnReliableConnected;
        nm.ReliableDisconnected += OnReliableDisconnected;
        nm.ReliableMessageReceived += OnReliableMessageReceived;
        nm.ReliableError += OnReliableError;

        nm.UnreliableConnected += OnUnreliableConnected;
        nm.UnreliableDisconnected += OnUnreliableDisconnected;
        nm.UnreliableMessageReceived += OnUnreliableMessageReceived;
        nm.UnreliableError += OnUnreliableError;

        // とりあえず TCP -> UDP の順で接続
        await nm.ConnectReliableAsync();
        await nm.ConnectUnreliableAsync();
    }

    private void CleanupCurrentRole()
    {
        foreach (var controller in _clientMouseControllers.Values)
        {
            Destroy(controller.gameObject);
        }
        _clientMouseControllers.Clear();

        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.ReliableConnected -= OnReliableConnected;
            nm.ReliableDisconnected -= OnReliableDisconnected;
            nm.ReliableMessageReceived -= OnReliableMessageReceived;
            nm.ReliableError -= OnReliableError;

            nm.UnreliableConnected -= OnUnreliableConnected;
            nm.UnreliableDisconnected -= OnUnreliableDisconnected;
            nm.UnreliableMessageReceived -= OnUnreliableMessageReceived;
            nm.UnreliableError -= OnUnreliableError;

            nm.DisconnectAll();
        }

        _tcpServer?.Stop();
        _tcpServer = null;

        _udpServer?.Stop();
        _udpServer = null;
    }

    #region Server EventsHandlers
    private async Task OnClientConnected(int id)
    {
        Debug.Log($"[TCP Server] Client {id} connected");
        var msg = new NetMessage<ChatPayload>
        {
            Type = NetMessageType.RegisteredClient,
            SenderId = 1, // サーバID
            TargetId = id,
            Payload = new ChatPayload { Text = "割り当ててやったぜ！" }
        };

        string json = NetJson.ToJson(msg);

        if(!_clientMouseControllers.ContainsKey(id))
        {
            Debug.Log($"Creating mouse controller for client {id}");
            var go = Instantiate(_clientMousePrefab);
            go.name = $"ClientMouse_{id}";
            go.transform.SetParent(_viewPanel.transform, false);
            var controller = go.GetComponent<ClientMouseController>();
            _clientMouseControllers.Add(id, controller);
        }

        await _tcpServer.Send(json);
    }

    private void OnClientDisconnected(int id)
    {
        Debug.Log($"[TCP Server] Client {id} disconnected");
        if(_clientMouseControllers.TryGetValue(id, out var controller))
        {
            Destroy(controller.gameObject);
            _clientMouseControllers.Remove(id);
        }
    }

    private void OnMessageReceived(int id, string msg)
    {
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            case NetMessageType.RegisteredClient:
                var netMsg = NetJson.FromJson<NetMessage<ChatPayload>>(msg);
                Debug.Log($"[Client Reliable]  Your ID is {netMsg.TargetId} {netMsg.Payload.Text}");
                break;
            case NetMessageType.MousePosition:
                var mousePos = NetJson.FromJson<NetMessage<MousePositionPayload>>(msg);
                if(_clientMouseControllers.TryGetValue(mousePos.SenderId, out var controller))
                {
                    controller.SetPosition(new Vector2(mousePos.Payload.X, mousePos.Payload.Y));
                }
                break;
            default:
                Debug.Log("[Client Reliable] (Unknown Role) " + msg);
                break;
        }
    }

    #endregion
    #region TCP/UDP EventsHandlers

    private void OnReliableConnected()
    {
        Debug.Log($"[Client Reliable] Connected as {_currentRole}");
    }

    private void OnReliableDisconnected()
    {
        Debug.Log("[Client Reliable] Disconnected");
    }

    private void OnReliableMessageReceived(string msg)
    {
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            case NetMessageType.RegisteredClient:
                var netMsg = NetJson.FromJson<NetMessage<ChatPayload>>(msg);
                Debug.Log($"[Client Reliable]  Your ID is {netMsg.TargetId} {netMsg.Payload.Text}");
                break;
            default:
                Debug.Log("[Client Reliable] (Unknown Role) " + msg);
                break;
        }
    }

    private void OnReliableError(System.Exception ex)
    {
        Debug.LogError("[Client Reliable] " + ex);
    }

    // --- Unreliable(UDP) イベント ---

    private void OnUnreliableConnected()
    {
        Debug.Log("[Client Unreliable] Ready (UDP)");
        // ここで座標同期開始などのフラグを立てるのもアリ
    }

    private void OnUnreliableDisconnected()
    {
        Debug.Log("[Client Unreliable] Disconnected");
    }

    private void OnUnreliableMessageReceived(string msg)
    {
        Debug.Log("[Client Unreliable] " + msg);
    }

    private void OnUnreliableError(System.Exception ex)
    {
        Debug.LogError("[Client Unreliable] " + ex);
    }
    #endregion
}
