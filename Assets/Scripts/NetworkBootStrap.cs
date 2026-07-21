using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using Michsky.UI.Shift;
using System.Net;
using System.Net.Sockets;
using EyeMoTMouseModule;

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

    [Header("Resources")]
    [SerializeField] private ClientManager _clientManagerPrefab;
    [SerializeField] private ServerManager _serverManagerPrefab;
    [SerializeField] private int maxClients = 4;
    [SerializeField] private ClientMouseController _clientMousePrefab;
    [SerializeField] private GameObject _clientROIPanelPrefab;
    [SerializeField] private CanvasScaler _viewCanvas;
    [SerializeField] private PlayerObject _playerObjectPrefab;
    public event Action ConnectedToServer;
    public event Action DisconnectedFromServer;
    private Dictionary<int, PlayerObject> _clients = new();
    private ClientManager _clientManager;
    private ServerManager _serverManager;
    private List<IServerCallbacks> _serverCallbacks;
    private List<IClientCallbacks> _clientCallbacks;

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public void AddServerCallbacks(IServerCallbacks callbacks)
    {
        if (!_serverCallbacks.Contains(callbacks))
        {
            _serverCallbacks.Add(callbacks);
        }
    }

    public void AddClientCallbacks(IClientCallbacks callbacks)
    {
        if (!_clientCallbacks.Contains(callbacks))
        {
            _clientCallbacks.Add(callbacks);
        }
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
        EyeMoTServerConnect.Instance.AddServer(GetLocalIPAddress(), port, "123");

        _currentRole = ClientManager.NetworkRole.Host;

        JoinView();
        // 自分もクライアントとして localhost に接続
        //await StartClientsAsync("127.0.0.1", port);
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
        DisconnectedFromServer?.Invoke();
        foreach(var obj in InterfaceManager.Instance.HostUIs)
        {
            obj?.SetActive(true);
        }
        Debug.Log("Disconnect / stop host");
        CleanupCurrentRole();
        RecordManager.Instance.Init();
        _currentRole = ClientManager.NetworkRole.None;
        EyeMoTServerConnect.Instance?.DeleteServer();
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
    }

    public PlayerObject CreateclientObjects(Vector2 screenSize, int idx, string ip)
    {
        var roi = Instantiate(_clientROIPanelPrefab, _viewCanvas.transform);
        _viewCanvas.referenceResolution = screenSize;
        roi.GetComponent<RectTransform>().sizeDelta = screenSize;
        var mouse = CurrentRole == ClientManager.NetworkRole.Host ? Instantiate(_clientMousePrefab, roi.transform) : null;
        var plObj = Instantiate(_playerObjectPrefab);
        plObj.Init(idx, ip, roi, mouse);
        return plObj;
    }

    private void CleanupCurrentRole()
    {
        foreach (var controller in _clients)
        {
            Destroy(controller.Value.MouseController.gameObject);
            Destroy(controller.Value.ROIPanel);
            Destroy(controller.Value.gameObject);
        }
        _clients.Clear();

        InterfaceManager.Instance.ViewPanelController.ClearAllImages();

        if(PlayerObject.Local != null)
        {
            Destroy(PlayerObject.Local.ROIPanel);
            Destroy(PlayerObject.Local.gameObject);
            PlayerObject.Local = null;
        }

        if (_clientManager != null)
        {
            _clientManager.RemoveAllCallbacks();
            _clientManager.Disconnect();
            Destroy(_clientManager.gameObject);
            _clientManager = null;
        }
        if (_serverManager != null)
        {
            _serverManager.RemoveAllListeners();
            _serverManager.Stop();
            Destroy(_serverManager.gameObject);
            _serverManager = null;
        }
    }

    public void JoinView()
    {
        ResourcesManager.Instance.Loading.SetActive(false);
        InterfaceManager.Instance.MainPanelManager.OpenPanel("View");
        foreach(var obj in InterfaceManager.Instance.HostUIs)
        {
            obj.SetActive(CurrentRole == ClientManager.NetworkRole.Host);
        }
        EyeMoTMouse.Instance.SetBlurImageActive(CurrentRole != ClientManager.NetworkRole.Host);
        InterfaceManager.Instance.ViewPanelUI.UpdateServerInfo();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ReturnButton()
    {
        Disconnect();
    } 

    public string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    #region ServerCallbacks
    void IServerCallbacks.OnClientConnected(TcpServer.ClientConnection client)
    {
        Debug.Log($"[Server] Client {client.Id} connected");
        ServerManager.Instance.SendTcp(
            NetJson.ToJson(new NetMessage<StringPayload>
            {
                Type = NetMessageType.RegisteredClient,
                SenderId = 1, // サーバID
                TargetId = client.Id,
                Payload = new StringPayload { Text = $"{client.Id}" }
            })
        );
    }

    void IServerCallbacks.OnClientDisconnected(TcpServer.ClientConnection client)
    {
        Debug.Log($"[Server] Client {client.Id} disconnected");
        if(_clients.TryGetValue(client.Id, out var playerObject))
        {
            Destroy(playerObject.MouseController.gameObject);
            Destroy(playerObject.ROIPanel);
            Destroy(playerObject.gameObject);
            _clients.Remove(client.Id);
        }   
        
    }

    void IServerCallbacks.OnTcpReceived(IPEndPoint ep, string msg)
    {
        //Debug.Log($"[Server] Message received from {ep}: {msg}");
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            // クライアントの画面サイズとマウスオブジェクト生成
            case NetMessageType.ClientObjectCreate:
                var rscrMsg = NetJson.FromJson<NetMessage<StringPayload>>(msg);
                Debug.Log($"[Client]  Client Screen size is {rscrMsg.Payload.Text}");
                if(!_clients.ContainsKey(rscrMsg.SenderId))
                {
                    var screenSizeParts = rscrMsg.Payload.Text.Split('x');
                    var screenSize = new Vector2(float.Parse(screenSizeParts[0]), float.Parse(screenSizeParts[1]));
                    var plobj = CreateclientObjects(screenSize, rscrMsg.SenderId, ep.Address.ToString());
                    _clients.Add(rscrMsg.SenderId, plobj);
                }
                break;
        }
    }

    void IServerCallbacks.OnUdpReceived(IPEndPoint ep, string msg)
    {
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            case NetMessageType.UdpConnectRequest:
                ClientSession udpSession;
                lock(_serverManager.Clients)
                {
                    if(_serverManager.Clients.TryGetValue(header.SenderId, out udpSession))
                    {
                        udpSession.Udp = ep;
                    }
                    else
                    {
                        Debug.LogWarning($"[Server] Client {header.SenderId} not found for UDP connect.");
                        return;
                    }
                }
                
                Debug.Log($"[Server] Client {header.SenderId} UDP connected from {ep}.");
                break;
            case NetMessageType.MousePosition:
                var mousePos = NetJson.FromJson<NetMessage<Vector3Payload>>(msg);
                if(_clients.TryGetValue(mousePos.SenderId, out var plObj))
                {
                    plObj.MouseController.SetPosition(new Vector2(mousePos.Payload.X, mousePos.Payload.Y));
                }
                break;
            default:
                Debug.Log($"[Server] (Unknown Role) {msg}");
                break;
        }
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
        Disconnect();
    }

    async void IClientCallbacks.OnTcpMessageReceived(string msg)
    {
        var header = NetJson.FromJson<NetMessage<object>>(msg);
        switch (header.Type)
        {
            case NetMessageType.RegisteredClient:
                //インデックス割り当て完了してからUDP接続開始
                var regiMsg = NetJson.FromJson<NetMessage<StringPayload>>(msg);
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

                JoinView();
                
                // ウィンドウサイズを取得
                var screenSize = new Vector2(Screen.width, Screen.height); 
                _clientManager.SendTcp(
                    NetJson.ToJson(new NetMessage<StringPayload>
                    {
                        Type = NetMessageType.ClientObjectCreate,
                        SenderId = _clientManager.Idx,
                        TargetId = -1, // サーバーへ送信
                        Payload = new StringPayload { Text = $"{screenSize.x}x{screenSize.y}" }
                    })
                );

                // ローカルプレイヤーオブジェクト生成
                CreateclientObjects(screenSize, _clientManager.Idx, GetLocalIPAddress());
                break;
            case NetMessageType.EffectCreate:
                var effectMsg = NetJson.FromJson<NetMessage<EffectCreatePayload>>(msg);
                var effectPosition = new Vector3(effectMsg.Payload.X, effectMsg.Payload.Y, effectMsg.Payload.Z);
                if(VFXManager.Instance.TryGet((VFXDef.TYPE)effectMsg.Payload.VFXTypeIndex, out var vfxData))
                {
                    Debug.Log($"Create effect {(VFXDef.TYPE)effectMsg.Payload.VFXTypeIndex} at {effectPosition} for client {effectMsg.SenderId}");
                    InterfaceManager.Instance.ViewPanelController.CreateEffectAt(vfxData.Data.Resource, effectPosition, effectMsg.Payload.CanPlaySE, effectMsg.Payload.CanPlayLowSE, PlayerObject.Local.ROIPanel.GetComponent<RectTransform>());
                }
                break;
            case NetMessageType.ImageCreate:
                var imageMsg = NetJson.FromJson<NetMessage<ImageCreatePayload>>(msg);
                var imagePosition = new Vector2(imageMsg.Payload.X, imageMsg.Payload.Y);
                if(ImageManager.Instance.TryGet(imageMsg.Payload.ImageKey, out var imageData))
                {
                    Debug.Log($"Create image {imageMsg.Payload.ImageKey} with animation {(ImageAnimationDef.TYPE)imageMsg.Payload.AnimationTypeIndex} at {imagePosition} for client {imageMsg.SenderId}");
                    InterfaceManager.Instance.ViewPanelController.CreateImageAt(imageData, imageMsg.Payload.ImageGUID, (ImageAnimationDef.TYPE)imageMsg.Payload.AnimationTypeIndex, imagePosition, PlayerObject.Local.ROIPanel.GetComponent<RectTransform>());
                }
                break;
            case NetMessageType.EyeMoTMouseStatus:
                var statusMsg = NetJson.FromJson<NetMessage<StringPayload>>(msg);
                Debug.Log($"EyeMoTMouse Status from client {statusMsg.SenderId}: {statusMsg.Payload.Text}");
                bool isTrackable = bool.Parse(statusMsg.Payload.Text);
                EyeMoTMouse.Instance.StatusChange(isTrackable);
                break;
            case NetMessageType.RecordStart:
                InterfaceManager.Instance.RecordTimer.StartCountDown();
                break;
            case NetMessageType.ImageDestroy:
                var destroyMsg = NetJson.FromJson<NetMessage<StringPayload>>(msg);
                Debug.Log($"Destroy image {destroyMsg.Payload.Text} for client {destroyMsg.SenderId}");
                InterfaceManager.Instance.ViewPanelController.ReceiveDestroyImage(destroyMsg.Payload.Text);
                break;
            case NetMessageType.ImageActive:
                var activeMsg = NetJson.FromJson<NetMessage<ImagePositionPayload>>(msg);
                InterfaceManager.Instance.ViewPanelController.ReceiveImageActive(activeMsg.Payload.ImageGUID);
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
            case NetMessageType.ImagePosition:
                var imagePos = NetJson.FromJson<NetMessage<ImagePositionPayload>>(msg);
                InterfaceManager.Instance.ViewPanelController.ReceiveImageAt(imagePos.Payload.ImageGUID, new Vector2(imagePos.Payload.X, imagePos.Payload.Y));
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
        InterfaceManager.Instance.MainPanelManager.OpenPanel("HostClientControll");
    }
    #endregion
}
