using System;
using System.Threading.Tasks;
using NativeWebSocket;

public class WebSocketNetworkClient
{
    private readonly string _url;
    private WebSocket _ws;

    public event Action Connected;
    public event Action Disconnected;
    public event Action<string> MessageReceived;
    public event Action<Exception> Error;

    public WebSocketNetworkClient(string url)
    {
        _url = url;
    }

    public async Task ConnectAsync()
    {
        _ws = new WebSocket(_url);

        _ws.OnOpen += () => Connected?.Invoke();
        _ws.OnError += (e) => Error?.Invoke(new Exception(e));
        _ws.OnClose += (code) => Disconnected?.Invoke();
        _ws.OnMessage += (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            MessageReceived?.Invoke(msg);
        };

        await _ws.Connect();
    }

    public async void Send(string message)
    {
        if (_ws == null) return;
        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
        await _ws.Send(bytes);
    }

    public async void Disconnect()
    {
        if (_ws == null) return;
        await _ws.Close();
        _ws = null;
    }

    public void Tick()
    {
        //WebGL ビルドでは JavaScript 側からイベントが飛んでくるので呼ばないよー
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
    }
}
