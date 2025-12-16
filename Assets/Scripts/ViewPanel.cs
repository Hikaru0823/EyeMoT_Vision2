using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewPanel : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Camera uiCamera;
    [SerializeField] float sendInterval = 0.02f; // 50fps
    float _timer;
    Vector2 _lastSent;
    bool _isSending = false;
    void OnEnable()
    {
        _isSending = true;
    }
    void OnDisable()
    {
        _isSending = false;
    }

    void Update()
    {
        if (!_isSending) return;

        _timer += Time.deltaTime;

        if (_timer < sendInterval) return;
        _timer = 0f;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            Input.mousePosition,
            uiCamera,
            out localPoint
        );

        // 一定以上動いてなければ送らない
        if ((localPoint - _lastSent).sqrMagnitude < 1f) // 1px未満
            return;

        _lastSent = localPoint;

        var msg = new NetMessage<MousePositionPayload>
        {
            Type = NetMessageType.MousePosition,
            SenderId = NetworkBootStrap.Instance.Idx,
            TargetId = 1,
            Payload = new MousePositionPayload { X = localPoint.x, Y = localPoint.y }
        };

        string json = NetJson.ToJson(msg);
        NetworkManager.Instance.SendUnreliable(json);
    }
}
