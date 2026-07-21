using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Michsky.UI.Shift;
using UnityEngine;
using UnityEngine.UI;

public class HostDiscoveryUI : MonoBehaviour
{
    [SerializeField] private Button _discoverHostsButton;
    [SerializeField] private Transform _hostsListParent;
    [SerializeField] private HostElementUI _hostElementPrefab;
    [SerializeField] private GameObject _noHostsFoundText;
    [SerializeField] private float pingInterval = 5f;
    [SerializeField] private float pingTimeout = 3f;
    [SerializeField] private int tcpConnectTimeoutMs = 1500;
    private List<HostElementUI> _hostElements = new List<HostElementUI>();
    private readonly List<Coroutine> _pingCoroutines = new List<Coroutine>();

    public void DiscoveryHostsFromServer()
    {
        ResourcesManager.Instance.Loading.SetActive(true);
        _noHostsFoundText.SetActive(false);
        StopPingCoroutines();
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

    async void OnReceived(List<ServerInfo> serverList)
    {
        await ShowReachableHostsAsync(serverList);
    }

    async Task ShowReachableHostsAsync(List<ServerInfo> serverList)
    {
        var reachableHosts = new List<ServerInfo>();
        foreach (var host in serverList)
        {
            if (await CanConnectTcpAsync(host.ip, host.port))
            {
                reachableHosts.Add(host);
            }
        }

        foreach (var host in reachableHosts)
        {
            var element = Instantiate(_hostElementPrefab, _hostsListParent);
            element.SetServerInfo(host, ClickAction);
            _hostElements.Add(element);
            _pingCoroutines.Add(StartCoroutine(UpdatePingLoop(element, host.ip)));
        }

        FinishDiscovery();
    }

    async Task<bool> CanConnectTcpAsync(string ipAddress, int port)
    {
        using (var client = new TcpClient())
        {
            try
            {
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(tcpConnectTimeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask != connectTask)
                {
                    Debug.Log($"TCP connect test timeout: {ipAddress}:{port}");
                    return false;
                }

                await connectTask;
                Debug.Log($"TCP connect test succeeded: {ipAddress}:{port}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.Log($"TCP connect test failed: {ipAddress}:{port} / {ex.Message}");
                return false;
            }
        }
    }

    void FinishDiscovery()
    {
        if (_hostElements.Count == 0)
        {
            _noHostsFoundText.SetActive(true);
        }
        ResourcesManager.Instance.Loading.SetActive(false);
        _discoverHostsButton.interactable = true;
    }

    IEnumerator UpdatePingLoop(HostElementUI element, string ipAddress)
    {
        while (element != null)
        {
            yield return PingHost(element, ipAddress);
            yield return new WaitForSeconds(pingInterval);
        }
    }

    IEnumerator PingHost(HostElementUI element, string ipAddress)
    {
        Ping ping = new Ping(ipAddress);
        float startedAt = Time.realtimeSinceStartup;

        while (!ping.isDone && Time.realtimeSinceStartup - startedAt < pingTimeout)
        {
            yield return null;
        }

        if (element != null)
        {
            element.UpdatePing(ping.isDone ? ping.time : -1);
            Debug.Log($"Ping to {ipAddress}: {(ping.isDone ? ping.time.ToString() : "Timeout")}");
        }

        ping.DestroyPing();
    }

    void StopPingCoroutines()
    {
        foreach (var coroutine in _pingCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        _pingCoroutines.Clear();
    }

    void OnDisable()
    {
        StopPingCoroutines();
    }
}
