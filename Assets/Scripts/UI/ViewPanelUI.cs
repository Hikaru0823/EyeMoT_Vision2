using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ViewPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI serverInfoText;

    public void UpdateServerInfo()
    {
        roleText.text = $"Role: {NetworkBootStrap.Instance.CurrentRole}";
        serverInfoText.text = ClientManager.Instance != null ?
            $"Connected to: {ClientManager.Instance.TCPHost}:{ClientManager.Instance.TCPPort}" :
            $"Connected to: {NetworkBootStrap.Instance.GetLocalIPAddress()}:{NetworkBootStrap.Instance.ServerManager.TcpServer.Port}";
    }
}
