using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface IServer
{
    void StartServer();
    void Stop();
    Task Send(string message);
    Task BroadcastAsync(string message);
    Task SendToClientAsync(int clientId, string message);

    event Action<int, string> MessageReceived;
    event Action<Exception> Error;
}