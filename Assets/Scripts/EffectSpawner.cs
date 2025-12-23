using Unity.VisualScripting;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [SerializeField] private EffectData _effectData;

    [Header("UI Resources")]
    [SerializeField] private Transform _viewPanel;

    public void SpawnEffect(EffectType effectType, Vector3 position)
    {
        if (_effectData.Effects.ContainsKey(effectType) && _effectData.Effects[effectType] != null)
        {
            var instance = Instantiate(_effectData.Effects[effectType], _viewPanel);
            instance.transform.position = position;
        }
    }
}