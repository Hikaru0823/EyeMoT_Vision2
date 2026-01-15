using System;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance { get; private set; }
    [SerializeField] private GameObject _loading;
    public GameObject Loading => _loading;
    [SerializeField] private ServerData _serverData;
    public ServerData ServerData  => _serverData;
    [SerializeField] private AudioListener _audioListener;
    public AudioListener AudioListener => _audioListener;


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