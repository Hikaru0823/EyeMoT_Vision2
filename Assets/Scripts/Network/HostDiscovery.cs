using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;

public static class HostDiscovery
{
    public static async Task<List<HostInfo>> DiscoverAsync(int discoveryPort, float timeoutSeconds = 3f)
    {
        var hosts = new List<HostInfo>();

        using (var udp = new UdpClient())
        {
            udp.EnableBroadcast = true;
            var request = new NetMessage<ChatPayload>
            {
                Type = NetMessageType.DiscoveryRequest,
                SenderId = 0,
                TargetId = -1,
                Payload = new ChatPayload { Text = "DISCOVER_GAME" }
            };

            var requestBytes = Encoding.UTF8.GetBytes(NetJson.ToJson(request));
            // ブロードキャスト送信
            await udp.SendAsync(requestBytes, requestBytes.Length, new IPEndPoint(System.Net.IPAddress.Broadcast, discoveryPort));


            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                if (udp.Available > 0)
                {
                    var result = await udp.ReceiveAsync();
                    var msg = NetJson.FromJson<NetMessage<object>>(Encoding.UTF8.GetString(result.Buffer));

                    if (msg.Type == NetMessageType.IAmHost)
                    {
                        var hostmsg = NetJson.FromJson<NetMessage<HostInfo>>(Encoding.UTF8.GetString(result.Buffer));
                        hosts.Add(hostmsg.Payload);
                    }
                }
                else
                {
                    await Task.Yield();
                }
            }
        }

        return hosts;
    }
}
