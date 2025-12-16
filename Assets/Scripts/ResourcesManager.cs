using System;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance { get; private set; }

    [SerializeField] private GameObject _loading;
    [SerializeField] private ServerData _serverData;
    public ServerData ServerData { get { return _serverData; } private set { _serverData = value; } }
    public GameObject Loading { get { return _loading; } private set { _loading = value; } }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
}