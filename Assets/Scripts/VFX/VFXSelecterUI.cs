using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VFXSelecterUI : SimpleHighlightSelecterOptionUI
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private VFXOption[] _VFXOptions;

    void Awake()
    {
        Init(_VFXOptions);
    }

    void OnEnable()
    {
        // 保存されているVFXを読み込み、選択状態にする
        var savedVFXType = ES3.Load<VFXDef.TYPE>(SaveKey.VFX_SELECTED, defaultValue: VFXDef.TYPE.FIRE_00);
        if(TryGetOption(savedVFXType, out var option))
        {
            OnOptionSelected(option);
        }
    }

    bool TryGetOption(VFXDef.TYPE type, out VFXOption option)
    {
        foreach(var opt in _VFXOptions)
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
        if(VFXManager.Instance != null && VFXManager.Instance.TryGet((option as VFXOption).Type, out var data))
        {
            VFXManager.Instance.SetCurrentVFX((option as VFXOption).Type);
            ES3.Save<VFXDef.TYPE>(SaveKey.VFX_SELECTED, (option as VFXOption).Type);
            _iconImage.sprite = option.PreviewImage.sprite;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAssign(_VFXOptions);
    }
#endif
}

[Serializable]
public class VFXOption : OptionButtonResources
{
    public VFXDef.TYPE Type;
}