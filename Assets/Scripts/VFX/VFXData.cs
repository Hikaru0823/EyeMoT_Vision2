using UnityEngine;

[System.Serializable]
public class VFXData
{
    [SerializeField]
    VFXDef.TYPE _Type;
    public VFXDef.TYPE Type => _Type;

    [SerializeField]
    VFXResource _Resource;
    public VFXResource Resource => _Resource;

    [SerializeField]
    public VFXProperty _Property;
    public VFXProperty Property => _Property;

}

[System.Serializable]
public class VFXResource
{
    [SerializeField]
    GameObject _Object;
    public GameObject Object => _Object;
    [SerializeField]
    string _SEPath;
    public string CurrentSEPath => _SEPath;
}

[System.Serializable]
public class VFXProperty
{
    [SerializeField]
    bool _CanPlaySE;
    public bool CanPlaySE => _CanPlaySE;

    [SerializeField]
    bool _CanPlayLowSE;
    public bool CanPlayLowSE => _CanPlayLowSE;
}
