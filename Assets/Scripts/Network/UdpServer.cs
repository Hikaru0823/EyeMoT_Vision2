using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class UdpServer : IServer
{
    private readonly int _port;
    private readonly int _discoveryPort;
    private UdpClient _udp;
    private UdpClient _discoveryUdp; // ホスト（UDPサーバー）があることをクライアントに知らせるためのUDPクライアント
    private CancellationTokenSource _cts;
    private Task _receiveTask;
    private Task _discoveryTask;

    // 送信元エンドポイント → クライアントID
    private readonly Dictionary<int, IPEndPoint> _clients = new Dictionary<int, IPEndPoint>();
    private int _nextClientId = 1;

    public event Action<int> ClientRegistered;
    public event Action<int, string> MessageReceived;
    public event Action<Exception> Error;

    public UdpServer(int port, int discoveryPort)
    {
        _port = port;
        _discoveryPort = discoveryPort;
    }

    public void StartServer()
    {
        if (_udp != null) return;

        _udp = new UdpClient(_port);
        _discoveryUdp = new UdpClient(_discoveryPort);
        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token, _udp));
        _discoveryTask = Task.Run(() => ReceiveLoopAsync(_cts.Token, _discoveryUdp));
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _udp?.Close(); } catch { }
        try { _discoveryUdp?.Close(); } catch { }

        _udp = null;
        _discoveryUdp = null;
        _cts?.Dispose();
        _cts = null;
        _receiveTask = null;
        _discoveryTask = null;

        _clients.Clear();
    }

    private async Task ReceiveLoopAsync(CancellationToken token, UdpClient udp)
    {
        try
        {
            int id = 0;
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await udp.ReceiveAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var ep = result.RemoteEndPoint;
                var msg = Encoding.UTF8.GetString(result.Buffer);

                if(NetJson.FromJson<NetMessage<object>>(msg).Type == NetMessageType.DiscoveryRequest)
                {
                    var responseMsg = new NetMessage<HostInfo>
                    {
                        Type = NetMessageType.IAmHost,
                        SenderId = -1, // サーバID
                        Payload = new HostInfo
                        {
                            Name = Dns.GetHostName(),
                            Address = ((IPEndPoint)_udp.Client.LocalEndPoint).Address.ToString(),
                            TcpPort = _port-1, // TCPポートはUDPポートの-1とする慣例
                            UdpPort = _port
                        }
                    };
                    var responseJson = NetJson.ToJson(responseMsg);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    try
                    {
                        await _discoveryUdp.SendAsync(responseBytes, responseBytes.Length, ep);
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(ex);
                    }
                    continue;
                }

                lock (_clients)
                {
                    if (!_clients.ContainsValue(ep))
                    {
                        id = _nextClientId++;
                        _clients.Add(id, ep);
                        ClientRegistered?.Invoke(id);
                    }
                }

                MessageReceived?.Invoke(id, msg);

                await Send(msg);
                // 受け取ったメッセージをそのまま全員にブロードキャストする例
                //await BroadcastAsync($"[UDP:{id}] {msg}");
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Error?.Invoke(ex);
        }
    }

    public async Task Send(string message)
    {
        var header = NetJson.FromJson<NetMessage<object>>(message);
        if(header.TargetId == 0)
        {
            await BroadcastAsync(message);
            return;
        }
        else
        {
            await SendToClientAsync(header.TargetId, message);
            return;
        }
    }

    public async Task BroadcastAsync(string message)
    {
        if (_udp == null) return;

        var data = Encoding.UTF8.GetBytes(message);

        List<IPEndPoint> snapshot;
        lock (_clients)
        {
            snapshot = new List<IPEndPoint>(_clients.Values);
        }

        foreach (var ep in snapshot)
        {
            try
            {
                await _udp.SendAsync(data, data.Length, ep);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }
    }

    public async Task SendToClientAsync(int clientId, string message)
    {
        if (_udp == null) return;

        IPEndPoint ep;
        lock (_clients)
        {
            if (!_clients.TryGetValue(clientId, out ep))
                return; // もう切断されてるなど
        }

        if (ep == null) return; // クライアントが見つからない

        var data = Encoding.UTF8.GetBytes(message);
        try
        {
            await _udp.SendAsync(data, data.Length, ep);
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
        }
    }
}
