using System.Collections;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.UI;

public class VFXPreview : MonoBehaviour
{
    [SerializeField] CanvasScaler _previewCanvas;
    [SerializeField] RawImage _previewImage;
    [SerializeField] RectTransform _previewParent;
    GameObject _currentVFXObject;
    string _currentPath;
    Coroutine _currentPreviewRoutine;

    public void PreviewVFX(VFXData vfxData)
    {
        Stop();

        _currentPreviewRoutine = StartCoroutine(PreviewRoutine(vfxData));
    }

    private IEnumerator PreviewRoutine(VFXData vfxData)
    {
        while(true)
        {
            var maxLength = Mathf.Max(_previewCanvas.referenceResolution.x, _previewCanvas.referenceResolution.y);
            var scale = (maxLength / 30) * (maxLength/_previewImage.rectTransform.rect.width) * Vector3.one;
            _currentVFXObject = InterfaceManager.Instance.ViewPanelController.CreateEffectAt(vfxData.Resource, Vector2.zero, vfxData.Property.CanPlaySE, vfxData.Property.CanPlayLowSE, _previewParent, scale);
            _currentPath = vfxData.Resource.CurrentSEPath;
            yield return new WaitForSeconds(3f);
        }
    }

    public void Stop()
    {
        if(_currentPreviewRoutine != null)
        {
            StopCoroutine(_currentPreviewRoutine);
            _currentPreviewRoutine = null;
        }
        if(_currentVFXObject != null)
        {
            Destroy(_currentVFXObject);
            _currentVFXObject = null;
        }
        SEManager.Instance.Stop(_currentPath);
    }
}