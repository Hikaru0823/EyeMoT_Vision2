using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ImageAnimations/ImageAnimationHolder")]
public class ImageAnimationHolder : ScriptableObject
{
    Dictionary<int, ImageAnimationData> _DataDictionary = new Dictionary<int, ImageAnimationData>();

    [SerializeField]
    ImageAnimationData[] _DataList = null;

    public void init()
    {
        _DataDictionary.Clear();
        foreach (var data in _DataList)
        {
            _DataDictionary.Add((int)data.Type, data);
        }
    }

    public bool TryGet(ImageAnimationDef.TYPE type, out ImageAnimationData data)
    {
        var result = _DataDictionary.TryGetValue((int)type, out data);
        return result;
    }

    public int Count => _DataList.Length;
}