using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Shift;
using UnityEngine;
using UnityEngine.UI;

public class HostClientControllerUI : MonoBehaviour
{
    [SerializeField] HostClientController _hostClientController;
    [SerializeField] private MainPanelManager _mainPanelManager;

    [Header("UI Buttons")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    //[SerializeField] private Button _disconnectButton;
    [SerializeField] private Button _discoverHostsButton;
    [SerializeField] private TMPro.TMP_Dropdown _hostsDropdown;

    private TcpServer _tcpServer;
    private UdpServer _udpServer;

    private void Awake()
    {
        _hostClientController.ConnectedToServer += OnConnectedToServer;

        _hostButton.onClick.AddListener(_hostClientController.StartHost);
        _clientButton.onClick.AddListener(StartClient);
        //_disconnectButton.onClick.AddListener(_hostClientController.Disconnect);
        _discoverHostsButton.onClick.AddListener(StartHostDiscovery);

        var nm = NetworkManager.Instance;
        nm.UnreliableMessageReceived += OnUnreliableMessageReceived;
    }

    private void OnUnreliableMessageReceived(string msg)
    {
        Debug.Log("Unreliable client connected");
    }

    private async void StartHostDiscovery()
    {
        _hostsDropdown.ClearOptions();
        var hosts = await HostDiscovery.DiscoverAsync(53000, 3f);
        List<string> options = new List<string>();
        foreach (var host in hosts)
        {
            options.Add($"{host.Name} ({host.Address}:{host.TcpPort})");
        }
        _hostsDropdown.AddOptions(options);
    }

    private void StartClient()
    {
        //ResourcesManager.Instance.Loading.SetActive(true);
        //_hostClientController.StartClient();
        _mainPanelManager.OpenPanel("Multiplayer");
    }

    private void OnConnectedToServer()
    {
        ResourcesManager.Instance.Loading.SetActive(false);
    }
}
