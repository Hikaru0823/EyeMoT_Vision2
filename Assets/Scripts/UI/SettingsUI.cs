using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Preview _vfxPreview;

    void OnDisable()
    {
        _vfxPreview.StopVFX();
    }

}