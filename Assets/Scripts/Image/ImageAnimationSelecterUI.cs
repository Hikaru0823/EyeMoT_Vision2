using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ImageAnimationSelecterUI : SimpleHighlightSelecterOptionUI
{
    //[SerializeField] private Image _iconImage;
    [SerializeField] private ImageAnimationOption[] _ImageAnimationOptions;
    [SerializeField] private UnityEvent<string> onAnimationChanged;

    void Awake()
    {
        Init(_ImageAnimationOptions);
    }

    void OnEnable()
    {
        // 保存されているVFXを読み込み、選択状態にする
        // var savedVFXType = ES3.Load<VFXDef.TYPE>(SaveKey.VFX_SELECTED, defaultValue: VFXDef.TYPE.FIRE_00);
        // if(TryGetOption(savedVFXType, out var option))
        // {
        //     OnOptionSelected(option);
        // }
        // if(InterfaceManager.Instance?.SelectorPanelManager.GetCurrentPanel() == "VFXSelect")
        // {
        //     InterfaceManager.Instance.SelectorPanelManager.OpenPanel("VFXSelect");
        // }
    }

    public void OnSelect()
    {
        var savedAnimationType = ES3.Load<ImageAnimationDef.TYPE>(SaveKey.ANIMATION_SELECTED, defaultValue: ImageAnimationDef.TYPE.Drag);
        if(TryGetOption(savedAnimationType, out var option))
        {
            OnOptionSelected(option);
        }
    }

    bool TryGetOption(ImageAnimationDef.TYPE type, out ImageAnimationOption option)
    {
        foreach(var opt in _ImageAnimationOptions)
        {
            if(opt.Type == type)
            {
                option = opt;
                return true;
            }
        }
        option = null;
        return false;
    }

    public override void OnOptionSelected(OptionButtonResources option)
    {
        base.OnOptionSelected(option);
        if(ImageManager.Instance != null && ImageManager.Instance.TryGetAnimation((option as ImageAnimationOption).Type, out var data))
        {
            ImageManager.Instance.SetCurrentAnimationType((option as ImageAnimationOption).Type);
            ES3.Save<ImageAnimationDef.TYPE>(SaveKey.ANIMATION_SELECTED, (option as ImageAnimationOption).Type);
            //_iconImage.sprite = option.PreviewImage.sprite;
            onAnimationChanged?.Invoke("Animation");
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAssign(_ImageAnimationOptions);
    }
#endif
}

[Serializable]
public class ImageAnimationOption : OptionButtonResources
{
    public ImageAnimationDef.TYPE Type;
}