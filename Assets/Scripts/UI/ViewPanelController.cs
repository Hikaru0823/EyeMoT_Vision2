using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViewPanelController : MonoBehaviour
{
    [Header("Canvas / Camera")]
    [SerializeField] private Canvas canvas;                 // Screen Space - Camera のCanvas
    [SerializeField] private GraphicRaycaster raycaster;    // Canvasに付いてる
    [SerializeField] private Camera uiCamera;               // CanvasのRender Camera

    [Header("Target")]
    [SerializeField] private string targetTag = "ViewPanel";
    [SerializeField] private string blockedTag = "BlockPanel";

    [Header("Mouse Sender")]
    [SerializeField] float sendInterval = 0.02f; // 50fps


    float _timer;
    Vector2 _lastSent;
    private PointerEventData _pointer;
    private readonly List<RaycastResult> _results = new();

    void Awake()
    {
        if (raycaster == null && canvas != null) raycaster = canvas.GetComponent<GraphicRaycaster>();

        if (uiCamera == null && canvas != null) uiCamera = canvas.worldCamera;
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        switch(NetworkBootStrap.Instance.CurrentRole)
        {
            case ClientManager.NetworkRole.Host:
                if (!Input.GetMouseButtonDown(0)) return;
                if (IsPointerOverBlockedUI()) return;
                if(!TryGetViewPanelAtPointer(out Vector2 pos, out RectTransform rect)) return;
                var opt = InterfaceManager.Instance.MainSelecterUI.GetCurrentSelected(out var sendableObj);
                if(!opt) return;
                CreateAt(sendableObj, pos, rect);
                SendAt(sendableObj, pos);
                break;
            case ClientManager.NetworkRole.Client:
                if(PlayerObject.Local == null) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    PlayerObject.Local.ViewPanel.GetComponent<RectTransform>(), Input.mousePosition, uiCamera, out Vector2 mousePos))
                return;

                _timer += Time.deltaTime;

                if (_timer < sendInterval) return;
                _timer = 0f;

                // 一定以上動いてなければ送らない
                if ((mousePos - _lastSent).sqrMagnitude < 1f) // 1px未満
                    return;

                _lastSent = mousePos;

                SendMousePosition(mousePos);
                break;
            default:
                break;
        }
    }

    public GameObject CreateAt(ISendable sendableObj, Vector2 position, RectTransform rect)
    {
        if (sendableObj is SendableVFX vfxOption)
            return CreateEffectAt(vfxOption.Data.Resource, position, vfxOption.Property.CanPlaySE, vfxOption.Property.CanPlayLowSE, rect);
        else if(sendableObj is SendableImage imageOption)
        {   
            return CreateImageAt(imageOption.Texture, position, rect);
        }
        return null;
    }

    void SendAt(ISendable sendableObj, Vector2 position)
    {
        if (sendableObj is SendableVFX vfxOption)
            SendEffectAt(vfxOption, position);
        else if (sendableObj is SendableImage imageOption)
        {
            SendImageAt(imageOption, position);
        }
    }

    void SendImageAt(SendableImage imageOption, Vector2 position)
    {
        var msg = new NetMessage<ImagePositionPayload>
        {
            Type = NetMessageType.ImagePosition,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 2,
            Payload = new ImagePositionPayload { ImageKey = imageOption.Key, X = position.x, Y = position.y }
        };
        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendTcp(json);
    }

    public GameObject CreateImageAt(Texture2D data, Vector2 position, RectTransform rect, Vector3 customScale = default)
    {
        var image = Instantiate(ImageManager.Instance.spritePrefab, rect.transform);
        image.GetComponent<Image>().sprite = Sprite.Create(data, new Rect(0, 0, data.width, data.height), new Vector2(0.5f, 0.5f));
        var maxLength = Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y);
        image.transform.localScale = (customScale == default) ? maxLength / 500 * Vector3.one : customScale; //300は基準サイズ
        image.transform.localPosition = position;
        var controller = image.GetComponent<ImageController>();
        if(controller == null)
        {
            Debug.LogError("ImageControllerが存在しません");
            return null;
        }
        //controller.init(3);
        return image;
    }

    void SendEffectAt(SendableVFX vFXOption, Vector2 position)
    {
        var msg = new NetMessage<EffectPositionPayload>
        {
            Type = NetMessageType.EffectPosition,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 2,
            Payload = new EffectPositionPayload { VFXTypeIndex = (int)vFXOption.Data.Type, CanPlaySE = vFXOption.Property.CanPlaySE, CanPlayLowSE = vFXOption.Property.CanPlayLowSE, X = position.x, Y = position.y, Z = 0 }
        };
        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendTcp(json);
    }

    public GameObject CreateEffectAt(VFXResource data, Vector2 position, bool canPlaySE, bool canPlayLowSE, RectTransform rect, Vector3 customScale = default)
    {
        var effect = Instantiate(data.Object, rect.transform);
        var maxLength = Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y);
        effect.transform.localScale = (customScale == default) ? maxLength / 30 * Vector3.one : customScale; //30は基準サイズ
        var effectPosition = (Vector3)position + Vector3.back *effect.transform.localScale.x*3; // 少し前に出す
        effect.transform.localPosition = effectPosition;
        var controller = effect.GetComponent<VFXController>();
        if(controller == null)
        {
            Debug.LogError("VFXControllerが存在しません");
            return null;
        }
        controller.init(data, canPlaySE, canPlayLowSE, isSpatial:(customScale == default));
        return effect;
    }

    void SendMousePosition(Vector2 position)
    {
        var msg = new NetMessage<MousePositionPayload>
        {
            Type = NetMessageType.MousePosition,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 1,
            Payload = new MousePositionPayload { X = position.x, Y = position.y }
        };
        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendUdp(json);
    }

    bool TryGetViewPanelAtPointer(out Vector2 _pos, out RectTransform _rect)
    {
        _pos = Vector2.zero;
        _rect = null;
        _pointer ??= new PointerEventData(EventSystem.current);
        _pointer.position = Input.mousePosition;

        _results.Clear();
        raycaster.Raycast(_pointer, _results);
        foreach (var r in _results)
        {
            if (!r.gameObject.CompareTag(targetTag)) continue;

            RectTransform rect = r.gameObject.GetComponent<RectTransform>();
            if (rect == null) return false;

            // Screen → World（RectTransformの平面上）
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, _pointer.position, uiCamera, out Vector2 localPivotOrigin))
                return false;

            _pos = localPivotOrigin;
            _rect = rect;
            return true;
        }
        return false;
    }

    bool IsPointerOverBlockedUI()
    {
        // "Panel UI"タグの特定チェック
        Vector2 screenPos = (Input.touchCount > 0) ? Input.GetTouch(0).position : Input.mousePosition;
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject != null && result.gameObject.CompareTag(blockedTag))
            {
                return true;
            }
        }

        return false;
    }
}
