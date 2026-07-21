using UnityEngine;

[System.Serializable]
public class ImageAnimationData
{
    [SerializeField]
    ImageAnimationDef.TYPE _Type;
    public ImageAnimationDef.TYPE Type => _Type;
}