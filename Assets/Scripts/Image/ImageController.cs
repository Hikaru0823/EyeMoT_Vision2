using KanKikuchi.AudioManager;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ImageController : MonoBehaviour
{
    Coroutine _PopCoroutine;
    Coroutine _MoveCoroutine;
    Coroutine _FlashCoroutine;
    Vector2 _LastSentPosition;
    bool _HasSentPosition = false;

    [SerializeField] float _MoveSendThreshold = 0.01f;
    [SerializeField] float _FlashInterval = 0.5f;
    [SerializeField] GameObject _destroyButton;
    [SerializeField] Image _image;
    public string ImageGUID { get; set; }
    private Vector3 _initialScale;
    private Vector2 _initialPosition;
    private bool _isDragging = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(NetworkBootStrap.Instance.CurrentRole != ClientManager.NetworkRole.Host)
        {
            _destroyButton.SetActive(false);
        }
    }

    public void Init(ImageAnimationDef.TYPE animationType, Vector3 initialScale)
    {
        _initialScale = initialScale;
        _initialPosition = transform.localPosition;
        switch(animationType)
        {
            case ImageAnimationDef.TYPE.Normal:
                break;
            case ImageAnimationDef.TYPE.Drag:
                transform.localScale = Vector3.zero;
                _isDragging = true;
                break;
            case ImageAnimationDef.TYPE.Flash:
                _FlashCoroutine = StartCoroutine(Flash());
                break;
            case ImageAnimationDef.TYPE.Pop:
                _PopCoroutine = StartCoroutine(PopAnimation());
                break;
        }
    }

    public void Active()
    {
        transform.localScale = _initialScale;
    }
    // Update is called once per frame
    void Update()
    {
        SendImageAtIfMoved(transform.position);

        if(_isDragging && Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            transform.localScale = _initialScale;
            if(InterfaceManager.Instance.ViewPanelController.TryGetViewPanelAtPointer(out Vector2 pos, out RectTransform rect))
            {
                NetworkBootStrap.Instance.ServerManager.SendTcp(
                    NetJson.ToJson(new NetMessage<ImagePositionPayload>
                    {
                        Type = NetMessageType.ImageActive,
                        SenderId = -1,
                        TargetId = -2, // サーバーへ送信
                        Payload = new ImagePositionPayload { ImageGUID = ImageGUID, X = pos.x, Y = pos.y }
                    })
                );
                _MoveCoroutine = StartCoroutine(MoveAt(_initialPosition, pos, 6f));
            }
        }
    }

    IEnumerator MoveAt(Vector2 startPosition, Vector2 endPosition, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            transform.localPosition = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = endPosition;
    }

    IEnumerator Flash()
    {
        if (_image == null)
        {
            yield break;
        }

        var interval = Mathf.Max(0.01f, _FlashInterval);
        bool isVisible = true;
        while (true)
        {
            yield return new WaitForSeconds(interval);
            isVisible = !isVisible;
            _image.color = isVisible ? Color.white : Color.clear;
        }
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
        var msg = new NetMessage<ImagePositionPayload>
        {
            Type = NetMessageType.ImagePosition,
            SenderId = -1,
            TargetId = -2,
            Payload = new ImagePositionPayload { ImageGUID = ImageGUID, X = position.x, Y = position.y }
        };
        string json = NetJson.ToJson(msg);
        NetworkBootStrap.Instance.ServerManager.SendUdp(json);
    }

    public void ReceiveImageAt(Vector2 position)
    {
        transform.position = position;
    }

    public void DestroyImage()
    {
        var msg = new NetMessage<StringPayload>
        {
            Type = NetMessageType.ImageDestroy,
            SenderId = -1,
            TargetId = -2,
            Payload = new StringPayload { Text = ImageGUID }
        };
        string json = NetJson.ToJson(msg);
        NetworkBootStrap.Instance.ServerManager.SendTcp(json);
        Destroy(gameObject);
    }

    private IEnumerator PopAnimation()
    {
        float duration = 0.3f;
        float elapsedTime = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = _initialScale;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }
}
