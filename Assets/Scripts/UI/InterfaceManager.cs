using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Michsky.UI.Shift;
using UnityEngine;

public class InterfaceManager : MonoBehaviour
{
    public static InterfaceManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    [SerializeField] private MainPanelManager _mainPanelManager;
    public MainPanelManager MainPanelManager => _mainPanelManager;
    [SerializeField] private MainPanelManager _selectorPanelManager;
    public MainPanelManager SelectorPanelManager => _selectorPanelManager;
    [SerializeField] private ViewPanelController _viewPanelController;
    public ViewPanelController ViewPanelController => _viewPanelController;
    [SerializeField] private ServerSettingUI _serverSettingUI;
    public ServerSettingUI ServerSettingUI => _serverSettingUI;
    [SerializeField] private HostDiscoveryUI _hostDiscoveryUI;
    public HostDiscoveryUI HostDiscoveryUI => _hostDiscoveryUI;
    [SerializeField] private MainSelecterUI _mainSelecterUI;
    public MainSelecterUI MainSelecterUI => _mainSelecterUI;
    [SerializeField] private GameObject[] _hostUIs;
    public GameObject[] HostUIs => _hostUIs;
    [SerializeField] private RecordTimer _recordTimer;
    public RecordTimer RecordTimer => _recordTimer;
}
