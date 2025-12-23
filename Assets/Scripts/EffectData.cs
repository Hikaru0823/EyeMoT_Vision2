using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Effect/Data")]
public class EffectData : ScriptableObject
{
    public GameObject Fire;

    public Dictionary<EffectType , GameObject> Effects = new Dictionary<EffectType, GameObject>();

    void OnEnable()
    {
        Effects[EffectType.Fire] = Fire;
    }
}

public enum EffectType
{
    Fire,
}