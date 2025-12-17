using Newtonsoft.Json;

public static class NetJson
{
    public static string ToJson(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    public static T FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}

public class ChatPayload
{
    public string Text;
}

public class MousePositionPayload
{
    public float X;
    public float Y;
}

public class NetMessage<TPayload>
{
    public string Type;
    public int SenderId;     // 送信元 clientId
    public int TargetId;     // 送信先 clientId （ブロードキャストの場合は0）
    public TPayload Payload;
}

public class NetMessageType
{
    public const string DiscoveryRequest = "DiscoveryRequest";
    public const string UdpConnectRequest = "UdpConnectRequest";
    public const string RegisteredClient = "RegisteredClient";
    public const string DisconnectedClient = "DisconnectedClient";
    public const string MouseCreate = "MouseCreate";
    public const string MousePosition = "MousePosition";
    public const string IAmHost = "IAmHost";
}
