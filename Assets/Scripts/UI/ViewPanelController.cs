using System;
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
    [SerializeField] private string[] blockedTags = {"BlockPanel", "ImagePanel"};
    [SerializeField] private string destroyImageTag = "DestroyImagePanel";

    [Header("Mouse Sender")]
    [SerializeField] float sendInterval = 0.02f; // 50fps


    float _timer;
    Vector2 _lastSent;
    RectTransform _draggingImage;
    RectTransform _draggingParent;
    Vector2 _dragOffset;
    private PointerEventData _pointer;
    private readonly List<RaycastResult> _results = new();
    private Dictionary<string, ImageController> _imageControllers = new();

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
                if (HandleDestroyImageButtonClick()) return;
                if (HandleImageDrag()) return;
                if (!Input.GetMouseButtonDown(0)) return;
                if (IsPointerOverBlockedUI()) return;
                if(!TryGetViewPanelAtPointer(out Vector2 pos, out RectTransform rect)) return;
                var opt = InterfaceManager.Instance.MainSelecterUI.GetCurrentSelected(out var sendableObj);
                if(!opt) return;
                var guid = Guid.NewGuid().ToString("N");
                CreateAt(sendableObj, pos, rect, guid);
                SendAt(sendableObj, pos, guid);
                break;
            case ClientManager.NetworkRole.Client:
                if(PlayerObject.Local == null) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    PlayerObject.Local.ROIPanel.GetComponent<RectTransform>(), Input.mousePosition, uiCamera, out Vector2 mousePos))
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

    public GameObject CreateAt(ISendable sendableObj, Vector2 position, RectTransform rect, string guid)
    {
        if (sendableObj is SendableVFX vfxOption)
            return CreateEffectAt(vfxOption.Data.Resource, position, vfxOption.Property.CanPlaySE, vfxOption.Property.CanPlayLowSE, rect);
        else if(sendableObj is SendableImage imageOption)
        {   
            var animationType = ImageManager.Instance.GetCurrentAnimationType();
            return CreateImageAt(imageOption.Texture, guid, animationType, position, rect);
        }
        return null;
    }

    void SendAt(ISendable sendableObj, Vector2 position, string guid)
    {
        if (sendableObj is SendableVFX vfxOption)
            SendEffectAt(vfxOption, position);
        else if (sendableObj is SendableImage imageOption)
        {
            var animationType = ImageManager.Instance.GetCurrentAnimationType();
            SendImageAt(imageOption, animationType, position, guid);
        }
    }

    void SendImageAt(SendableImage imageOption, ImageAnimationDef.TYPE animationType, Vector2 position, string guid)
    {
        var msg = new NetMessage<ImageCreatePayload>
        {
            Type = NetMessageType.ImageCreate,
            SenderId = -1,
            TargetId = -2,
            Payload = new ImageCreatePayload { ImageKey = imageOption.Key, ImageGUID = guid, X = position.x, Y = position.y, AnimationTypeIndex = (int)animationType }
        };
        string json = NetJson.ToJson(msg);
        NetworkBootStrap.Instance.ServerManager.SendTcp(json);
    }

    public void ReceiveImageAt(string imageGUID, Vector2 position)
    {
        if(_imageControllers.TryGetValue(imageGUID, out var controller))
        {
            controller.ReceiveImageAt(position);
        }
    }

    public void ReceiveDestroyImage(string imageGUID)
    {
        if(_imageControllers.TryGetValue(imageGUID, out var controller))
        {
            Destroy(controller.gameObject);
            _imageControllers.Remove(imageGUID);
        }
    }

    public void ReceiveImageActive(string imageGUID)
    {
        if(_imageControllers.TryGetValue(imageGUID, out var controller))
        {
            controller.Active();
        }
    }

    public GameObject CreateImageAt(Texture2D data, string imageGUID, ImageAnimationDef.TYPE animationType, Vector2 position, RectTransform rect, Vector3 customScale = default)
    {
        var image = Instantiate(ImageManager.Instance.spritePrefab, rect.transform);
        image.GetComponent<Image>().sprite = Sprite.Create(data, new Rect(0, 0, data.width, data.height), new Vector2(0.5f, 0.5f));
        var maxLength = Mathf.Max(rect.rect.size.x, rect.rect.size.y);
        var scale = (customScale == default) ? maxLength / 500 * Vector3.one : customScale; //500は基準サイズ
        image.transform.localScale = scale;
        image.transform.localPosition = position;
        var controller = image.GetComponent<ImageController>();
        if(controller == null)
        {
            Debug.LogError("ImageControllerが存在しません");
            return null;
        }
        controller.Init(animationType, scale);
        controller.ImageGUID = imageGUID;
        _imageControllers.Add(imageGUID, controller);
        return image;
    }

    void SendEffectAt(SendableVFX vFXOption, Vector2 position)
    {
        var msg = new NetMessage<EffectCreatePayload>
        {
            Type = NetMessageType.EffectCreate,
            SenderId = -1,
            TargetId = -2,
            Payload = new EffectCreatePayload { VFXTypeIndex = (int)vFXOption.Data.Type, CanPlaySE = vFXOption.Property.CanPlaySE, CanPlayLowSE = vFXOption.Property.CanPlayLowSE, X = position.x, Y = position.y, Z = 0 }
        };
        string json = NetJson.ToJson(msg);
        NetworkBootStrap.Instance.ServerManager.SendTcp(json);
    }

    public GameObject CreateEffectAt(VFXResource data, Vector2 position, bool canPlaySE, bool canPlayLowSE, RectTransform rect, Vector3 customScale = default)
    {
        var effect = Instantiate(data.Object, rect.transform);
        var maxLength = Mathf.Max(rect.rect.size.x, rect.rect.size.y);
        effect.transform.localScale = (customScale == default) ? maxLength / 30 * Vector3.one : customScale; //30は基準サイズ
        var effectPosition = (Vector3)position + Vector3.back *effect.transform.localScale.x*3; // 少し前に出す
        effect.transform.localPosition = effectPosition;
        var controller = effect.GetComponent<VFXController>();
        if(controller == null)
        {
            Debug.LogError("VFXControllerが存在しません");
            return null;
        }
        controller.init(data, canPlaySE, canPlayLowSE, isSpatial: customScale == default);
        return effect;
    }

    void SendMousePosition(Vector2 position)
    {
        var msg = new NetMessage<Vector3Payload>
        {
            Type = NetMessageType.MousePosition,
            SenderId = ClientManager.Instance.Idx,
            TargetId = -1,
            Payload = new Vector3Payload { X = position.x, Y = position.y }
        };
        string json = NetJson.ToJson(msg);
        ClientManager.Instance.SendUdp(json);
    }

    public bool TryGetViewPanelAtPointer(out Vector2 _pos, out RectTransform _rect)
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
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, _pointer.position, uiCamera, out Vector2 localPivotOrigin))
                return false;

            _pos = localPivotOrigin;
            _rect = rect;
            return true;
        }
        return false;
    }

    bool HandleDestroyImageButtonClick()
    {
        if (!Input.GetMouseButtonDown(0)) return false;

        _pointer ??= new PointerEventData(EventSystem.current);
        _pointer.position = Input.mousePosition;

        _results.Clear();
        EventSystem.current.RaycastAll(_pointer, _results);

        foreach (RaycastResult result in _results)
        {
            if (result.gameObject == null) continue;
            if (!result.gameObject.CompareTag(destroyImageTag)) continue;

            Button button = result.gameObject.GetComponent<Button>();
            if (button == null)
            {
                button = result.gameObject.GetComponentInParent<Button>();
            }

            if (button == null || !button.IsInteractable()) return true;

            button.onClick.Invoke();
            return true;
        }

        return false;
    }

    bool HandleImageDrag()
    {
        if (_draggingImage != null)
        {
            if (Input.GetMouseButton(0))
            {
                MoveDraggingImage(Input.mousePosition);
                return true;
            }

            _draggingImage = null;
            _draggingParent = null;
            return true;
        }

        if (!Input.GetMouseButtonDown(0)) return false;
        if (!TryGetImageAtPointer(out RectTransform imageRect)) return false;

        _draggingImage = imageRect;
        _draggingParent = imageRect.parent as RectTransform;
        if (_draggingParent == null)
        {
            _draggingImage = null;
            return false;
        }

        if (TryGetLocalPointInDraggingParent(Input.mousePosition, out Vector2 localPoint))
        {
            _dragOffset = imageRect.anchoredPosition - localPoint;
        }
        else
        {
            _dragOffset = Vector2.zero;
        }

        return true;
    }

    void MoveDraggingImage(Vector2 screenPosition)
    {
        if (_draggingImage == null || _draggingParent == null) return;
        if (!TryGetLocalPointInDraggingParent(screenPosition, out Vector2 localPoint)) return;

        _draggingImage.anchoredPosition = localPoint + _dragOffset;
    }

    bool TryGetImageAtPointer(out RectTransform imageRect)
    {
        imageRect = null;
        _pointer ??= new PointerEventData(EventSystem.current);
        _pointer.position = Input.mousePosition;

        _results.Clear();
        EventSystem.current.RaycastAll(_pointer, _results);

        foreach (RaycastResult result in _results)
        {
            if (result.gameObject == null) continue;
            if (result.gameObject.CompareTag("BlockPanel")) return false;

            if (!result.gameObject.CompareTag("ImagePanel")) continue;
            if (!result.gameObject.TryGetComponent(out ImageController _)) continue;

            imageRect = result.gameObject.GetComponent<RectTransform>();
            return imageRect != null;
        }

        return false;
    }

    bool TryGetLocalPointInDraggingParent(Vector2 screenPosition, out Vector2 localPoint)
    {
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = uiCamera != null ? uiCamera : canvas.worldCamera;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _draggingParent,
            screenPosition,
            eventCamera,
            out localPoint);
    }

    public void ClearAllImages()
    {
        foreach (var controller in _imageControllers.Values)
        {
            Destroy(controller.gameObject);
        }
        _imageControllers.Clear();
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

            if (result.gameObject != null && System.Array.Exists(blockedTags, tag => result.gameObject.CompareTag(tag)))
            {
                return true;
            }
        }

        return false;
    }
}
