using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientMouseController : MonoBehaviour
{
    void Start()
    {
        transform.SetParent(InterfaceManager.Instance.viewPanel.mouseContent, false);
    }
    public void SetPosition(Vector2 position)
    {
        transform.localPosition = position;
    }
}
