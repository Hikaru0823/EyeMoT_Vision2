using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Shift;
using UnityEngine;
using UnityEngine.UI;

public class HostClientControllerUI : MonoBehaviour
{
    [SerializeField] NetworkBootStrap _hostClientController;
    [SerializeField] private MainPanelManager _mainPanelManager;
    [SerializeField] private ServerSettingUI _serverSettingUI;

    [Header("UI Buttons")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    //[SerializeField] private Button _disconnectButton;
    [SerializeField] private Button _discoverHostsButton;

    private TcpServer _tcpServer;
    private UdpServer _udpServer;

    private void Awake()
    {
        _hostButton.onClick.AddListener(StartHost);
        _clientButton.onClick.AddListener(StartClient);
        //_disconnectButton.onClick.AddListener(_hostClientController.Disconnect);

        var nm = NetworkManager.Instance;
    }


    private void StartClient()
    {

        //_mainPanelManager.OpenPanel("Multiplayer");
    }

    private void StartHost()
    {
        _serverSettingUI.Init(port => _hostClientController.StartHost(port));
    }
}
