using System;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance { get; private set; }

    [SerializeField] private GameObject _loading;
    [SerializeField] private ServerData _serverData;
    [SerializeField] private VFXHolder _vfxHolder;
    public ServerData ServerData  => _serverData;
    public VFXHolder VFXHolder => _vfxHolder;
    public GameObject Loading => _loading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        _vfxHolder.init();
    }
}