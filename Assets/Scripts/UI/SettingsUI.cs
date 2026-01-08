using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private VFXPreview _vfxPreview;

    void OnDisable()
    {
        _vfxPreview.Stop();
    }

}