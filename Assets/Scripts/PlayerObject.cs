using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    public static PlayerObject Local = null;
    public ClientMouseController MouseController;
    public GameObject ROIPanel;
    public int Id;
    public string Ip;

    public void Init(int id, string ip, GameObject roiPanel, ClientMouseController mouseController)
    {
        Id = id;
        Ip = ip;
        ROIPanel = roiPanel;
        MouseController = mouseController;
        if(ClientManager.Instance?.Idx == id)
        {
            Local = this;
        }
    }
}
