using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VFXSelecterUI : SimpleHighlightSelecterOptionUI
{
    [SerializeField] private VFXOption[] vfxOptions;
    public VFXData CurrentVFXData {get; private set;}

    void Start()
    {
        Init(vfxOptions);
    }

    public override void OnOptionSelected(OptionButtonResources option)
    {
        base.OnOptionSelected(option);
        if(ResourcesManager.Instance.VFXHolder.TryGet((option as VFXOption).TYPE, out var data))
        {
            Debug.Log($"VFX Selected: {(option as VFXOption).TYPE}, SEPath: {data.Resource.CurrentSEPath}");
            CurrentVFXData = data;
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// インスペクターで値が変更された時に呼ばれる（エディタのみ）
    /// </summary>
    void OnValidate()
    {
        AutoAssign(vfxOptions);
    }
    #endif
}

[Serializable]
public class VFXOption : OptionButtonResources
{
    public VFXDef.TYPE TYPE;
}