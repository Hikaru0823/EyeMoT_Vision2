using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }
    [SerializeField] private VFXHolder _vfxHolder;
    [SerializeField, ReadOnly] private SendableVFX[] _VFXList;
    [SerializeField, ReadOnly] private SendableVFX CurrentVFX;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        _vfxHolder.init();
        Init();
    }

    void Init()
    {
        _VFXList = new SendableVFX[_vfxHolder.Count];
        int index = 0;
        foreach(var def in VFXDef.TYPE.GetValues(typeof(VFXDef.TYPE)))
        {
            if(_vfxHolder.TryGet((VFXDef.TYPE)def, out var data))
            {
                SendableVFX vfx = new SendableVFX();
                vfx.Data = data;
                vfx.Property = new VFXProperty(); 
                _VFXList[index] = vfx;
                index++;
            }
        }
    }

    public bool TryGet(VFXDef.TYPE type, out SendableVFX result)
    {
        foreach(var vfx in _VFXList)
        {
            if(vfx.Data.Type == type)
            {
                result = vfx;
                return true;
            }
        }
        result = null;
        return false;
    }
    public void ChangeOption_SEState(VFXDef.TYPE type, bool isOn)
    {
        if(TryGet(type, out var option))
        {
            option.Property.CanPlaySE = isOn;
        }
    }

    public void ChangeOption_LowSEState(VFXDef.TYPE type, bool isOn)
    {
        if(TryGet(type, out var option))
        {
            option.Property.CanPlayLowSE = isOn;
        }
    }

    public SendableVFX GetCurrentVFX()
    {
        return CurrentVFX;
    }
    public void SetCurrentVFX(VFXDef.TYPE type)
    {
        if(TryGet(type, out var vfx))
        {
            CurrentVFX = vfx;
        }
    }
}

public class SendableVFX : ISendable
{
    public VFXData Data;
    public VFXProperty Property;
}
