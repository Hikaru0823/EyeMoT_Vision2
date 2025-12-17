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
    public event Action<IPEndPoint, string> MessageReceived;
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
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        _discoveryTask = Task.Run(() => ReceiveDiscoveryLoopAsync(_cts.Token));
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
    }

    public void Tick()
    {
        // UDPはコネクションレスなので特にやることなし
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udp.ReceiveAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var ep = result.RemoteEndPoint;
                var msg = Encoding.UTF8.GetString(result.Buffer);

                MessageReceived?.Invoke(ep, msg);
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Error?.Invoke(ex);
        }
    }

    private async Task ReceiveDiscoveryLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _discoveryUdp.ReceiveAsync().ConfigureAwait(false);
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
                            Address = GetLocalIPAddress(),
                            TcpPort = _port-1, // TCPポートはUDPポートの-1とする慣例
                            UdpPort = _port,
                            //Players = _clients.Count
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
                }
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Error?.Invoke(ex);
        }
    }

    // public async Task PingSend(int clientId, string message)
    // {
    //     await SendToClientAsync(clientId, message);
    // }

    public async Task BroadcastAsync(List<ClientSession> targetClients, string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        foreach (var client in targetClients)
        {
            try
            {
                await _udp.SendAsync(data, data.Length, client.Udp);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }
    
    }

    public async Task SendToClientAsync(ClientSession targetClient, string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        try
        {
            await _udp.SendAsync(data, data.Length, targetClient.Udp);
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
