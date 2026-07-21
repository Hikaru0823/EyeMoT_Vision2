using System.Collections;
using System.Collections.Generic;
using Kirurobo;
using UnityEngine;
using UnityEngine.UI;

public class TranparentManager : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] private Sprite[] _transparentIcon; // 0: Transparent, 1: Opaque
    [SerializeField] private Image _transparentButtonImage;

    void Start()
    {
        // 透明アイコンを設定
        UpdateTransparentIcon();
    }

    private void UpdateTransparentIcon()
    {
        if(_transparentButtonImage != null)
        {
            _transparentButtonImage.sprite = UniWindowController.current.isTransparent ? _transparentIcon[1] : _transparentIcon[0];
        }
    }

    public void OnTransparentButtonPressed()
    {
        UniWindowController.current.isTransparent = !UniWindowController.current.isTransparent;
        PlayerObject.Local.ROIPanel.GetComponent<Image>().color = UniWindowController.current.isTransparent ? new Color(1, 1, 1, 0.0f) : Color.white;
        UpdateTransparentIcon();
    }
}
