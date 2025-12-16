using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettingUI : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Dropdown _portDropdown;

    private Action<int> OnStartHost;

    public void Init(Action<int> onStartHost = null, List<string> addOptions = null)
    {
        _portDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var port in ResourcesManager.Instance.ServerData.PortPresets)
        {
            options.Add(port.ToString());
        }
        _portDropdown.AddOptions(options);
        if (addOptions != null)
        {
            _portDropdown.AddOptions(addOptions);
        }
        if (onStartHost != null)
        {
            OnStartHost += onStartHost;
        }
    }

    public void StartHost()
    {
        Debug.Log($"Start Host on port {int.Parse(_portDropdown.options[_portDropdown.value].text)}");
        OnStartHost?.Invoke(int.Parse(_portDropdown.options[_portDropdown.value].text));
    }
}
