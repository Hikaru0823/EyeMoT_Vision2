
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VFXOptionButon : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Resources")]
        public RawImage previewImage;
        public Image background;
        public VFXPreview preview;

        [Header("Preview")]
        [SerializeField] VFXDef.TYPE _type;

        [TextArea] public string description;
        public Texture imageTexture;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(ResourcesManager.Instance.VFXHolder.TryGet(_type, out var data))
                preview.PreviewVFX(data);
        }
    }