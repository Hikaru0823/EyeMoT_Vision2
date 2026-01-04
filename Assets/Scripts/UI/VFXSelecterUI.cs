using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VFXSelecterUI : SimpleHighlightSelecterOptionUI
{
    [SerializeField] private VFXOption[] vfxOptions;
    public VFXDef.TYPE CurrentVFXType{get; private set;}

    void Start()
    {
        Init(vfxOptions);
    }

    public override void OnOptionSelected(OptionButtonResources option)
    {
        base.OnOptionSelected(option);
        CurrentVFXType = (option as VFXOption).TYPE;
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