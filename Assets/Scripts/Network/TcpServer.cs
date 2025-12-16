using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public class TcpServer : IServer
{
    private readonly int _port;
    private TcpListener _listener;
    private CancellationTokenSource _cts;

    private readonly Dictionary<int, ClientConnection> _clients = new Dictionary<int, ClientConnection>();
    private int _nextClientId = 1;

    private class ClientConnection
    {
        public int Id;
        public TcpClient Client;
        public StreamReader Reader;
        public StreamWriter Writer;
        public Task ReceiveTask;
    }

    public event Action<int> ClientConnected;
    public event Action<int> ClientDisconnected;
    public event Action<int, string> MessageReceived;
    public event Action<Exception> Error;

    public TcpServer(int port)
    {
        _port = port;
    }

    public void StartServer()
    {
        if (_listener != null) return;

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        // 接続待ちループ
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();

        lock (_clients)
        {
            foreach (var c in _clients.Values)
            {
                try { c.Reader?.Dispose(); } catch { }
                try { c.Writer?.Dispose(); } catch { }
                try { c.Client?.Close(); } catch { }
            }
            _clients.Clear();
        }

        try { _listener?.Stop(); } catch { }

        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding.UTF8);
                var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                var conn = new ClientConnection
                {
                    Id = _nextClientId++,
                    Client = client,
                    Reader = reader,
                    Writer = writer
                };

                lock (_clients)
                {
                    if(!_clients.TryGetValue(conn.Id, out var _))
                    {
                        _clients.Add(conn.Id, conn);
                    }
                }

                ClientConnected?.Invoke(conn.Id);

                conn.ReceiveTask = Task.Run(() => ReceiveLoopAsync(conn, token));
            }
        }
        catch (ObjectDisposedException)
        {
            // Stop() された場合
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Error?.Invoke(ex);
        }
    }

    private async Task ReceiveLoopAsync(ClientConnection conn, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var line = await conn.Reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null) break;

                MessageReceived?.Invoke(conn.Id, line);

                await Send(line);
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Error?.Invoke(ex);
        }
        finally
        {
            DisconnectClient(conn);
        }
    }

    private void DisconnectClient(ClientConnection conn)
    {
        lock (_clients)
        {
            _clients.Remove(conn.Id);
        }

        try { conn.Reader?.Dispose(); } catch { }
        try { conn.Writer?.Dispose(); } catch { }
        try { conn.Client?.Close(); } catch { }

        ClientDisconnected?.Invoke(conn.Id);
    }

    public async Task Send(string message)
    {
        var header = NetJson.FromJson<NetMessage<object>>(message);
        Debug.Log($"[TCP Server] Sending message to TargetId={header.TargetId}");
        if(header.TargetId == 0)
        {
            await BroadcastAsync(message);
            return;
        }
        else if(header.TargetId == -1)
        {
            // サーバへの送信は無視
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
        List<ClientConnection> snapshot;
        lock (_clients)
        {
            snapshot = new List<ClientConnection>(_clients.Values);
        }

        foreach (var c in snapshot)
        {
            try
            {
                await c.Writer.WriteLineAsync(message);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }
    }

    public async Task SendToClientAsync(int clientId, string message)
    {
        ClientConnection conn;
        lock (_clients)
        {
            if (!_clients.TryGetValue(clientId, out conn))
                return; // もう切断されてるなど
        }

        try
        {
            await conn.Writer.WriteLineAsync(message);
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
        }
    }
}
