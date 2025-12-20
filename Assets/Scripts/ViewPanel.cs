using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewPanel : MonoBehaviour
{
    public Transform mouseContent;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI serverInfoText;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Camera uiCamera;
    [SerializeField] float sendInterval = 0.02f; // 50fps
    float _timer;
    Vector2 _lastSent;
    bool _isSending = false;
    void OnEnable()
    {
        if(NetworkBootStrap.Instance.CurrentRole == ClientManager.NetworkRole.Client)
        {
            _isSending = true;
        }
        else
        {
        }
        roleText.text = $"Role: {NetworkBootStrap.Instance.CurrentRole}";
        serverInfoText.text = ClientManager.Instance != null ?
            $"Connected to: {ClientManager.Instance.TCPHost}:{ClientManager.Instance.TCPPort}" :
            "Not connected";
    }
    void OnDisable()
    {
        _isSending = false;
    }

    void Update()
    {
        if (!_isSending) return;
        if(ClientManager.Instance == null)
        {
            _isSending = false;
            return;
        }

        _timer += Time.deltaTime;

        if (_timer < sendInterval) return;
        _timer = 0f;

        Vector2 localPoint = Input.mousePosition;
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(
        //     panelRect,
        //     Input.mousePosition,
        //     uiCamera,
        //     out localPoint
        // );

        // 一定以上動いてなければ送らない
        if ((localPoint - _lastSent).sqrMagnitude < 1f) // 1px未満
            return;

        _lastSent = localPoint;

        var msg = new NetMessage<MousePositionPayload>
        {
            Type = NetMessageType.MousePosition,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 1,
            Payload = new MousePositionPayload { X = localPoint.x, Y = localPoint.y }
        };

        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendUdp(json);
    }
}
