
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageSettingButton : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Resources")]
        public Preview preview;

        [Header("Preview")]
        [SerializeField] VFXDef.TYPE _type;

        public void OnPointerEnter(PointerEventData eventData)
        {
            PreviewVFX();
        }

        public void ChangeVFXSEState(bool isOn)
        {
            VFXManager.Instance.ChangeOption_SEState(_type, isOn);
            PreviewVFX();
        }

        public void ChangeVFXLowSEState(bool isOn)
        {
            VFXManager.Instance.ChangeOption_LowSEState(_type, isOn);
            PreviewVFX();
        }

        private void PreviewVFX()
        {
            Debug.Log("Preview VFX:" + _type);
            if (VFXManager.Instance.TryGet(_type, out var data) && InterfaceManager.Instance.MainPanelManager.GetCurrentPanelName() == "Settings")
                preview.PreviewVFX(data);
        }
    }