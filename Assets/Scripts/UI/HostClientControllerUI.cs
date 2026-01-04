using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Shift;
using UnityEngine;
using UnityEngine.UI;

public class HostClientControllerUI : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;

    private void Awake()
    {
        _hostButton.onClick.AddListener(StartHost);
        _clientButton.onClick.AddListener(StartClient);
    }


    private void StartClient()
    {
        InterfaceManager.Instance.HostDiscoveryUI.DiscoveryHosts();
    }

    private void StartHost()
    {
        InterfaceManager.Instance.ServerSettingUI.Init(port => NetworkBootStrap.Instance.StartHost(port));
    }
}
