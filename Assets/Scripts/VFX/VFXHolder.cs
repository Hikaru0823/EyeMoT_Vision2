using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VFX/VFXHolder")]
public class VFXHolder : ScriptableObject
{
    Dictionary<int, VFXData> _DataDictionary = new Dictionary<int, VFXData>();

    [SerializeField]
    VFXData[] _DataList = null;

    public void init()
    {
        _DataDictionary.Clear();
        foreach (var data in _DataList)
        {
            _DataDictionary.Add((int)data.Type, data);
        }
    }

    public bool TryGet(VFXDef.TYPE type, out VFXData data)
    {
        var result = _DataDictionary.TryGetValue((int)type, out data);
        return result;
    }

    public int Count => _DataList.Length;
}