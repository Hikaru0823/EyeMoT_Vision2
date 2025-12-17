using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public interface IServerCallbacks
{
    public void OnClientConnected(TcpServer.ClientConnection client);
    public void OnClientDisconnected(TcpServer.ClientConnection client);
    public void OnMessageReceived(IPEndPoint ep, string msg);
    public void OnTcpError(System.Exception ex);
    public void OnUdpError(System.Exception ex);
}