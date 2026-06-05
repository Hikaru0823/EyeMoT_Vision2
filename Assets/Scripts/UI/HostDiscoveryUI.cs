using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Shift;
using UnityEngine;
using UnityEngine.UI;

public class HostDiscoveryUI : MonoBehaviour
{
    [SerializeField] private Button _discoverHostsButton;
    [SerializeField] private Transform _hostsListParent;
    [SerializeField] private HostElementUI _hostElementPrefab;
    [SerializeField] private GameObject _noHostsFoundText;
    private List<HostElementUI> _hostElements = new List<HostElementUI>();

    public async void DiscoveryHosts()
    {
        ResourcesManager.Instance.Loading.SetActive(true);
        _noHostsFoundText.SetActive(false);
        foreach (var element in _hostElements)
        {
            Destroy(element.gameObject);
        }
        _hostElements.Clear();

        _discoverHostsButton.interactable = false;
        try
        {
            var hosts = await HostDiscovery.DiscoverAsync(ResourcesManager.Instance.ServerData.DictionaryPort_UDP, 3f);
            foreach (var host in hosts)
            {
                var element = Instantiate(_hostElementPrefab, _hostsListParent);
                element.SetHostInfo(host, ClickAction);
                _hostElements.Add(element);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Host discovery failed: {ex.Message}");
        }
        finally
        {
            if (_hostElements.Count == 0)
            {
                _noHostsFoundText.SetActive(true);
            }
            ResourcesManager.Instance.Loading.SetActive(false);
            _discoverHostsButton.interactable = true;
        }
    }

    public void DiscoveryHostsFromServer()
    {
        ResourcesManager.Instance.Loading.SetActive(true);
        _noHostsFoundText.SetActive(false);
        foreach (var element in _hostElements)
        {
            Destroy(element.gameObject);
        }
        _hostElements.Clear();

        _discoverHostsButton.interactable = false;
        try
        {
            EyeMoTServerConnect.Instance.GetServerList(OnReceived);
        }
        catch (System.Exception ex)
        {
            if (_hostElements.Count == 0)
            {
                _noHostsFoundText.SetActive(true);
            }
            ResourcesManager.Instance.Loading.SetActive(false);
            _discoverHostsButton.interactable = true;
        }
    }

    void ClickAction(string ipAddress, int port)
    {
        NetworkBootStrap.Instance.StartClient(ipAddress, port);
    }

    void OnReceived(List<ServerInfo> serverList)
    {
        foreach (var host in serverList)
        {
            var element = Instantiate(_hostElementPrefab, _hostsListParent);
            element.SetServerInfo(host, ClickAction);
            _hostElements.Add(element);
        }

        if (_hostElements.Count == 0)
        {
            _noHostsFoundText.SetActive(true);
        }
        ResourcesManager.Instance.Loading.SetActive(false);
        _discoverHostsButton.interactable = true;
    }
}
