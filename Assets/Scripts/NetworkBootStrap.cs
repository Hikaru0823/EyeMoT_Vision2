using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using Michsky.UI.Shift;
using System.Net;

public class NetworkBootStrap : MonoBehaviour, IClientCallbacks, IServerCallbacks
{
    public static NetworkBootStrap Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    [Header("State")]
    [SerializeField, ReadOnly] private ClientManager.NetworkRole _currentRole = ClientManager.NetworkRole.None;
    public ClientManager.NetworkRole CurrentRole => _currentRole;
    [SerializeField] private MainPanelManager _mainPanelManager;

    [Header("Resources")]
    [SerializeField] private EffectSpawner _effectSpawner;
    [SerializeField] private ClientManager _clientManagerPrefab;
    [SerializeField] private ServerManager _serverManagerPrefab;
    [SerializeField] private int maxClients = 4;
    [SerializeField] private ClientMouseController _clientMousePrefab;
    [SerializeField] private GameObject _clientViewPanelPrefab;
    [SerializeField] private CanvasScaler _viewCanvas;
    [SerializeField] private PlayerObject _playerObjectPrefab;
    PlayerObject _localPlayerObject;
    public event Action ConnectedToServer;
    private Dictionary<int, PlayerObject> _clients = new();
    private ClientManager _clientManager;
    private ServerManager _serverManager;

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public async void StartHost(int port)
    {
        Debug.Log("Switch role to Host");

        CleanupCurrentRole();

        // --- サーバ起動（TCP + UDP） ---
        if(_serverManager != null)
        {
            Destroy(_serverManager.gameObject);
            _serverManager = null;
        }
        
        _serverManager = Instantiate(_serverManagerPrefab);
        
        var _tcpServer = new TcpServer(port);
        var _usdServer = new UdpServer(port + 1, ResourcesManager.Instance.ServerData.DictionaryPort_UDP);
        _serverManager.AddListener((IServerCallbacks)this);
        _serverManager.InitializeTcp(_tcpServer);
        _serverManager.InitializeUdp(_usdServer);
        _serverManager.StartTcp();
        _serverManager.StartUdp();

        Debug.Log($"Servers started: TCP:{port}, UDP:{port + 1}");

        _currentRole = ClientManager.NetworkRole.Host;
        // 自分もクライアントとして localhost に接続
        await StartClientsAsync("127.0.0.1", port);
    }

    public async void StartClient(string ipAdress, int port)
    {
        Debug.Log("Switch role to Client");

        CleanupCurrentRole();

        _currentRole = ClientManager.NetworkRole.Client;
        await StartClientsAsync(ipAdress, port);

        ConnectedToServer?.Invoke();
    }

    public void Disconnect()
    {
        Debug.Log("Disconnect / stop host");
        CleanupCurrentRole();
        _currentRole = ClientManager.NetworkRole.None;
    }

    private async Task StartClientsAsync(string hostIp, int port)
    {
        var tcp = new TcpNetworkClient(hostIp, port);
        var udp = new UdpNetworkClient(hostIp, port + 1);

        if(_clientManager != null)
        {
            Destroy(_clientManager.gameObject);
            _clientManager = null;
        }
        _clientManager = Instantiate(_clientManagerPrefab);
        _clientManager.AddCallbacks((IClientCallbacks)this);
        _clientManager.InitializeTcp(tcp);
        _clientManager.InitializeUdp(udp);

        // とりあえず TCP -> UDP の順で接続
        ResourcesManager.Instance.Loading.SetActive(true);
        await _clientManager.ConnectTcpAsync();
        //await _clientManager.ConnectUdpAsync();
    }

    private void CleanupCurrentRole()
    {
        foreach (var controller in _clients.Values)
        {
            Destroy(controller.gameObject);
        }
        _clients.Clear();

        if (_clientManager != null)
        {
            _clientManager.RemoveCallbacks((IClientCallbacks)this);
            _clientManager.Disconnect();
            Destroy(_clientManager.gameObject);
            _clientManager = null;
        }
        if (_serverManager != null)
        {
            _serverManager.RemoveListener((IServerCallbacks)this);
            _serverManager.Stop();
            Destroy(_serverManager.gameObject);
            _serverManager = null;
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    #region ServerCallbacks
    void IServerCallbacks.OnClientConnected(TcpServer.ClientConnection client)
    {
        Debug.Log($"[Server] Client {client.Id} connected");
        ServerManager.Instance.SendTcp(
            NetJson.ToJson(new NetMessage<ChatPayload>
            {
                Type = NetMessageType.RegisteredClient,
                SenderId = 1, // サーバID
                TargetId = client.Id,
                Payload = new ChatPayload { Text = $"{client.Id}" }
            })
        );
    }

    void IServerCallbacks.OnClientDisconnected(TcpServer.ClientConnection client)
    {
        Debug.Log($"[Server] Client {client.Id} disconnected");
        if(_clients.TryGetValue(client.Id, out var playerObject))
        {
            Destroy(playerObject.MouseController.gameObject);
            Destroy(playerObject.ViewPanel);
            Destroy(playerObject.gameObject);
            _clients.Remove(client.Id);
        }   
        
    }

    void IServerCallbacks.OnMessageReceived(IPEndPoint ep, string msg)
    {
        //Debug.Log($"[Server] Message received from {ep}: {msg}");
    }

    void IServerCallbacks.OnTcpError(System.Exception ex)
    {
        Debug.LogError("[Server TCP] " + ex);
    }
    void IServerCallbacks.OnUdpError(System.Exception ex)
    {
        Debug.LogError("[Server UDP] " + ex);
    }

    #endregion
    #region ClientCallbacks

    void IClientCallbacks.OnTcpConnected()
    {
        Debug.Log($"[Client] Connected as {_currentRole}");
    }

    void IClientCallbacks.OnTcpDisconnected()
    {
        Debug.Log("[Client Reliable] Disconnected");
    }

    async void IClientCallbacks.OnTcpMessageReceived(string msg)
    {
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            case NetMessageType.RegisteredClient:
                //インデックス割り当て完了してからUDP接続開始
                var regiMsg = NetJson.FromJson<NetMessage<ChatPayload>>(msg);
                Debug.Log($"[Client]  Your ID is {regiMsg.Payload.Text}");
                _clientManager.Idx = int.Parse(regiMsg.Payload.Text);
                await _clientManager.ConnectUdpAsync();

                _clientManager.SendUdp(
                    NetJson.ToJson(new NetMessage<object>
                    {
                        Type = NetMessageType.UdpConnectRequest,
                        SenderId = _clientManager.Idx,
                        TargetId = -1, // サーバーへ送信
                        Payload = null
                    })
                );

                ResourcesManager.Instance.Loading.SetActive(false);
                _mainPanelManager.OpenPanel("View");

                if(CurrentRole == ClientManager.NetworkRole.Host) return;
                // ウィンドウサイズを取得
                var screenSize = $"{Screen.width}x{Screen.height}";
                _clientManager.SendTcp(
                    NetJson.ToJson(new NetMessage<ChatPayload>
                    {
                        Type = NetMessageType.ScreenSize,
                        SenderId = _clientManager.Idx,
                        TargetId = 1, // ホストへ送信
                        Payload = new ChatPayload { Text = screenSize }
                    })
                );
                var vp = Instantiate(_clientViewPanelPrefab, _viewCanvas.transform);
                _viewCanvas.referenceResolution = new Vector2(Screen.width, Screen.height);
                vp.GetComponent<RectTransform>().sizeDelta = _viewCanvas.referenceResolution;
                vp.GetComponent<RectTransform>().localPosition = new Vector2(-Screen.width/2, -Screen.height/2);
                _localPlayerObject = Instantiate(_playerObjectPrefab);
                _localPlayerObject.ViewPanel = vp;
                _localPlayerObject.Id = _clientManager.Idx;
                break;
            case NetMessageType.ScreenSize:
                var rscrMsg = NetJson.FromJson<NetMessage<ChatPayload>>(msg);
                Debug.Log($"[Client]  Client Screen size is {rscrMsg.Payload.Text}");
                var screenSizeParts = rscrMsg.Payload.Text.Split('x');
                if(!_clients.ContainsKey(rscrMsg.SenderId)&&rscrMsg.SenderId!=1)
                {
                    var vpGo = Instantiate(_clientViewPanelPrefab, _viewCanvas.transform);
                    _viewCanvas.referenceResolution = new Vector2(
                    float.Parse(screenSizeParts[0]),
                    float.Parse(screenSizeParts[1])
                    );
                    vpGo.GetComponent<RectTransform>().sizeDelta = _viewCanvas.referenceResolution;
                    vpGo.GetComponent<RectTransform>().localPosition = new Vector2(-float.Parse(screenSizeParts[0])/2, -float.Parse(screenSizeParts[1])/2);
                    var go = Instantiate(_clientMousePrefab, vpGo.transform);
                    go.name = $"ClientMouse_{rscrMsg.SenderId}";
                    var controller = go.GetComponent<ClientMouseController>();
                    var plobj = Instantiate(_playerObjectPrefab);
                    plobj.ViewPanel = vpGo;
                    plobj.MouseController = controller;
                    plobj.Id = rscrMsg.SenderId;
                    _clients.Add(rscrMsg.SenderId, plobj);
                }
                break;
            case NetMessageType.EffectPosition:
                var effectMsg = NetJson.FromJson<NetMessage<EffectPositionPayload>>(msg);
                var effectPosition = new Vector3(effectMsg.Payload.X, effectMsg.Payload.Y, effectMsg.Payload.Z);
                _effectSpawner.SpawnEffect(effectMsg.Payload.EffectType, effectPosition);
                break;
            default:
                Debug.Log("[Client Reliable] (Unknown Role) " + msg);
                break;
        }
    }

    void IClientCallbacks.OnTcpError(System.Exception ex)
    {
        Debug.LogError("[Client Reliable] " + ex);
        Disconnect();
    }

    // --- Unreliable(UDP) イベント ---

    void IClientCallbacks.OnUdpConnected()
    {
        Debug.Log("[Client Unreliable] Ready (UDP)");
        // ここで座標同期開始などのフラグを立てるのもアリ
    }

    void IClientCallbacks.OnUdpDisconnected()
    {
        Debug.Log("[Client Unreliable] Disconnected");
    }

    void IClientCallbacks.OnUdpReceived(string msg)
    {
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            case NetMessageType.MousePosition:
                var mousePos = NetJson.FromJson<NetMessage<MousePositionPayload>>(msg);
                if(_clients.TryGetValue(mousePos.SenderId, out var plObj))
                {
                    plObj.MouseController.SetPosition(new Vector2(mousePos.Payload.X, mousePos.Payload.Y));
                }
                break;
            default:
                Debug.Log("[Client Unreliable] (Unknown Role) " + msg);
                break;
        }
    }

    void IClientCallbacks.OnUdpError(System.Exception ex)
    {
        Debug.LogError("[Client Unreliable] " + ex);
        Disconnect();
        _mainPanelManager.OpenPanel("HostClientControll");
    }
    #endregion
}
