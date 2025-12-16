using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientMouseController : MonoBehaviour
{
    void Start()
    {
        Debug.Log("ClientMouseController started");
    }
    public void SetPosition(Vector2 position)
    {
        transform.localPosition = position;
    }
}
