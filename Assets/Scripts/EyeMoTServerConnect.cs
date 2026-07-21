using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class EyeMoTServerConnect : Singleton<EyeMoTServerConnect>
{
    [SerializeField] private string url = "https://www.poran.net/eyemot/_TEST_/server_api.php";
    [SerializeField] private string globalIpFallbackUrl = "https://api.ipify.org";
    public List<ServerInfo> serverList = new List<ServerInfo>();
    private string _currentIp;
    private int _currentPort;
    private string _currentPassword;

    //protected override bool PersistAcrossScenes => true;

    public void AddServer(string ip, int port, string password)
    {
        _currentIp = ip;
        _currentPort = port;
        _currentPassword = password;
        StartCoroutine(AddServerCoroutine(ip, port, password));
    }

    private IEnumerator AddServerCoroutine(string ip, int port, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "add");
        form.AddField("ip", ip);
        form.AddField("port", port.ToString());
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            Debug.Log(request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
        }
    }

    public void DeleteServer(string ip, int port, string password)
    {
        StartCoroutine(DeleteServerCoroutine(ip, port, password));
    }

    public void DeleteServer()
    {
        if(_currentIp == null || _currentPort == 0 || _currentPassword == null)
        {
            Debug.LogError("サーバー情報が設定されていません。");
            return;
        }
        StartCoroutine(DeleteServerCoroutine(_currentIp, _currentPort, _currentPassword));
    }

    private IEnumerator DeleteServerCoroutine(string ip, int port, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "delete");
        form.AddField("ip", ip);
        form.AddField("port", port.ToString());
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            Debug.Log(request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
        }
    }

    public void GetServerList(System.Action<List<ServerInfo>> onComplete)
    {
        StartCoroutine(GetServerListCoroutine(onComplete));
    }

    public void GetGlobalIPAddress(System.Action<string> onComplete)
    {
        StartCoroutine(GetGlobalIPAddressCoroutine(onComplete));
    }

    private IEnumerator GetGlobalIPAddressCoroutine(System.Action<string> onComplete)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "global_ip");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(request.downloadHandler.text.Trim());
            }
            else
            {
                Debug.LogWarning("Global IP fetch from server failed. Trying fallback: " + request.error);
                Debug.LogWarning(request.downloadHandler.text);
                yield return GetGlobalIPAddressFallbackCoroutine(onComplete);
            }
        }
    }

    private IEnumerator GetGlobalIPAddressFallbackCoroutine(System.Action<string> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(globalIpFallbackUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(request.downloadHandler.text.Trim());
            }
            else
            {
                Debug.LogError("Global IP fallback fetch failed: " + request.error);
                Debug.LogError(request.downloadHandler.text);
                onComplete?.Invoke("");
            }
        }
    }

    private IEnumerator GetServerListCoroutine(System.Action<List<ServerInfo>> onComplete)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "list");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string csvText = request.downloadHandler.text;

                List<ServerInfo> list = ParseServerCsv(csvText);

                onComplete?.Invoke(list);
            }
            else
            {
                Debug.LogError("CSV取得失敗: " + request.error);
                Debug.LogError(request.downloadHandler.text);

                onComplete?.Invoke(new List<ServerInfo>());
            }
        }
    }

    private List<ServerInfo> ParseServerCsv(string csvText)
    {
        List<ServerInfo> result = new List<ServerInfo>();

        if (string.IsNullOrEmpty(csvText))
        {
            return result;
        }

        string[] lines = csvText.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            // ヘッダー行をスキップ
            if (line.StartsWith("created_at"))
            {
                continue;
            }

            string[] columns = line.Split(',');

            if (columns.Length < 4)
            {
                Debug.LogWarning("CSV形式が不正です: " + line);
                continue;
            }

            string createdAt = columns[0];
            string ip = columns[1];
            string portText = columns[2];
            string password = columns[3];
            string globalIp = columns.Length >= 5 ? columns[4] : "";

            if (!int.TryParse(portText, out int port))
            {
                Debug.LogWarning("portをintに変換できません: " + portText);
                continue;
            }

            ServerInfo info = new ServerInfo(createdAt, ip, port, password, globalIp);
            result.Add(info);
        }

        return result;
    }
}
[System.Serializable]
public class ServerInfo
{
    public string createdAt;
    public string ip;
    public int port;
    public string password;
    public string globalIp;

    public ServerInfo(string createdAt, string ip, int port, string password, string globalIp = "")
    {
        this.createdAt = createdAt;
        this.ip = ip;
        this.port = port;
        this.password = password;
        this.globalIp = globalIp;
    }
}
