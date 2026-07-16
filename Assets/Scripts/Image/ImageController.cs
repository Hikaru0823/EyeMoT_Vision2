using KanKikuchi.AudioManager;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
public class ImageController : MonoBehaviour
{
    bool _AliveCheckEnable = false;
    float _AliveTimer = -1;
    Coroutine _PopCoroutine;
    Coroutine _DisappearCoroutine;
    bool _IsDisappearing = false;
    Vector2 _LastSentPosition;
    bool _HasSentPosition = false;

    [SerializeField] float _PopStartScale = 0.8f;
    [SerializeField] float _PopOvershootScale = 1.08f;
    [SerializeField] float _PopInDuration = 0.12f;
    [SerializeField] float _PopOutDuration = 0.08f;
    [SerializeField] float _DisappearEndScale = 0.7f;
    [SerializeField] float _DisappearDuration = 0.12f;
    [SerializeField] float _MoveSendThreshold = 0.01f;
    [SerializeField] GameObject _destroyButton;
    public string ImageGUID { get; set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(NetworkBootStrap.Instance.CurrentRole != ClientManager.NetworkRole.Host)
        {
            _destroyButton.SetActive(false);
        }
    }

    public void init(float aliveTimer = -1)
    {
        _AliveTimer = aliveTimer;
        if (0 < aliveTimer)
        {
            _AliveCheckEnable = true;
        }

        playPopScale();
    }

    // Update is called once per frame
    void Update()
    {
        SendImageAtIfMoved(transform.position);
        aliveCheck();
    }

    void SendImageAtIfMoved(Vector2 position)
    {
        if (NetworkBootStrap.Instance.CurrentRole != ClientManager.NetworkRole.Host)
        {
            return;
        }

        if (string.IsNullOrEmpty(ImageGUID))
        {
            return;
        }

        if (!_HasSentPosition)
        {
            _LastSentPosition = position;
            _HasSentPosition = true;
            return;
        }

        if ((position - _LastSentPosition).sqrMagnitude < _MoveSendThreshold * _MoveSendThreshold)
        {
            return;
        }

        _LastSentPosition = position;
        SendImageAt(position);
    }

    void SendImageAt(Vector2 position)
    {
        var msg = new NetMessage<ImageDynamicPositionPayload>
        {
            Type = NetMessageType.ImageDynamicPosition,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 2,
            Payload = new ImageDynamicPositionPayload { ImageGUID = ImageGUID, X = position.x, Y = position.y }
        };
        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendUdp(json);
    }

    public void ReceiveImageAt(Vector2 position)
    {
        transform.position = position;
    }

    void aliveCheck()
    {
        if (!_AliveCheckEnable)
        {
            return;
        }

        _AliveTimer -= Time.deltaTime;
        if (_AliveTimer < 0 && !_IsDisappearing)
        {
            _AliveCheckEnable = false;
            playDisappearScale();
        }
    }

    void playPopScale()
    {
        if (_PopCoroutine != null)
        {
            StopCoroutine(_PopCoroutine);
        }
        _PopCoroutine = StartCoroutine(popScaleRoutine());
    }

    IEnumerator popScaleRoutine()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 startScale = baseScale * _PopStartScale;
        Vector3 overshootScale = baseScale * _PopOvershootScale;

        transform.localScale = startScale;

        float t = 0f;
        while (t < _PopInDuration)
        {
            t += Time.deltaTime;
            float ratio = _PopInDuration <= 0f ? 1f : Mathf.Clamp01(t / _PopInDuration);
            transform.localScale = Vector3.Lerp(startScale, overshootScale, ratio);
            yield return null;
        }

        t = 0f;
        while (t < _PopOutDuration)
        {
            t += Time.deltaTime;
            float ratio = _PopOutDuration <= 0f ? 1f : Mathf.Clamp01(t / _PopOutDuration);
            transform.localScale = Vector3.Lerp(overshootScale, baseScale, ratio);
            yield return null;
        }

        transform.localScale = baseScale;
        _PopCoroutine = null;
    }

    void playDisappearScale()
    {
        _IsDisappearing = true;
        if (_PopCoroutine != null)
        {
            StopCoroutine(_PopCoroutine);
        }
        if (_DisappearCoroutine != null)
        {
            StopCoroutine(_DisappearCoroutine);
        }
        _DisappearCoroutine = StartCoroutine(disappearScaleRoutine());
    }

    IEnumerator disappearScaleRoutine()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 endScale = baseScale * _DisappearEndScale;

        float t = 0f;
        while (t < _DisappearDuration)
        {
            t += Time.deltaTime;
            float ratio = _DisappearDuration <= 0f ? 1f : Mathf.Clamp01(t / _DisappearDuration);
            transform.localScale = Vector3.Lerp(baseScale, endScale, ratio);
            yield return null;
        }

        transform.localScale = endScale;
        Destroy(gameObject);
    }

    public void DestroyImage()
    {
        var msg = new NetMessage<ChatPayload>
        {
            Type = NetMessageType.ImageDestroy,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 2,
            Payload = new ChatPayload { Text = ImageGUID }
        };
        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendTcp(json);
        Destroy(gameObject);
    }
}
