using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViewPanelUiRaycast : MonoBehaviour
{
    [Header("Canvas / Camera")]
    [SerializeField] private Canvas canvas;                 // Screen Space - Camera のCanvas
    [SerializeField] private GraphicRaycaster raycaster;    // Canvasに付いてる
    [SerializeField] private Camera uiCamera;               // CanvasのRender Camera

    [Header("Target")]
    [SerializeField] private string targetTag = "ViewPanel";

    private PointerEventData _pointer;
    private readonly List<RaycastResult> _results = new();

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (raycaster == null && canvas != null) raycaster = canvas.GetComponent<GraphicRaycaster>();

        // Screen Space - Camera の場合は canvas.worldCamera を使うのが安全
        if (uiCamera == null && canvas != null) uiCamera = canvas.worldCamera;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current == null) return;
        if(NetworkBootStrap.Instance.CurrentRole == ClientManager.NetworkRole.Client) return;

        _pointer ??= new PointerEventData(EventSystem.current);
        _pointer.position = Input.mousePosition;

        _results.Clear();
        raycaster.Raycast(_pointer, _results);
        foreach (var r in _results)
        {
            if (!r.gameObject.CompareTag(targetTag)) continue;

            RectTransform rect = r.gameObject.GetComponent<RectTransform>();
            if (rect == null) return;

            // Screen → World（RectTransformの平面上）
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, _pointer.position, uiCamera, out Vector2 localPivotOrigin))
                return;
            Debug.Log($"local Point on ViewPanel: {localPivotOrigin}");
            var effect = Instantiate(ResourcesManager.Instance.FireworksPrefab, rect.transform);
            var effectPosition = (Vector3)localPivotOrigin + Vector3.back * 0.1f; // 少し前に出す
            effect.transform.localPosition = effectPosition;
            var msg = new NetMessage<EffectPositionPayload>
            {
                Type = NetMessageType.EffectPosition,
                SenderId = ClientManager.Instance.Idx,
                TargetId = 2,
                Payload = new EffectPositionPayload { Id = ClientManager.Instance.Idx, X = effectPosition.x, Y = effectPosition.y, Z = effectPosition.z }
            };

            string json = NetJson.ToJson(msg);
            ClientManager.Instance.SendTcp(json);

            return; // 最前面の ViewPanel のみ
        }
    }
}
