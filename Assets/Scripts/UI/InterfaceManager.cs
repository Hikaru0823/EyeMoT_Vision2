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

    [SerializeField] private ViewPanelController _viewPanelController;
    [SerializeField] private ServerSettingUI _serverSettingUI;
    [SerializeField] private HostDiscoveryUI _hostDiscoveryUI;
    [SerializeField] private VFXSelecterUI _vfxSelecterUI;
    public ViewPanelController ViewPanelController => _viewPanelController;
    public ServerSettingUI ServerSettingUI => _serverSettingUI;
    public HostDiscoveryUI HostDiscoveryUI => _hostDiscoveryUI;
    public VFXSelecterUI VFXSelecterUI => _vfxSelecterUI;
}
