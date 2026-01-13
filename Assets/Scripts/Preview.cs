using System.Collections;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.UI;

public class Preview : MonoBehaviour
{
    [SerializeField] CanvasScaler _previewCanvas;
    [SerializeField] RawImage _previewImage;
    [SerializeField] RectTransform _previewParent;
    GameObject _currentVFXObject;
    string _currentPath;
    Coroutine _currentPreviewRoutine;
    Texture _initImage;

    public void Awake()
    {
        _initImage = _previewImage.texture;
    }

    public void PreviewImage(Texture2D tex)
    {
        StopVFX();

        _previewImage.texture = tex;
    }

    public void PreviewVFX(VFX opt)
    {
        StopVFX();

        _previewImage.texture = _initImage;
        _currentPreviewRoutine = StartCoroutine(PreviewVFXRoutine(opt));
    }

    private IEnumerator PreviewVFXRoutine(VFX opt)
    {
        while(true)
        {
            var maxLength = Mathf.Max(_previewCanvas.referenceResolution.x, _previewCanvas.referenceResolution.y);
            var scale = (maxLength / 30) * (maxLength/_previewImage.rectTransform.rect.width) * Vector3.one;
            _currentVFXObject = InterfaceManager.Instance.ViewPanelController.CreateEffectAt(opt.Data.Resource, Vector2.zero, opt.Property.CanPlaySE, opt.Property.CanPlayLowSE, _previewParent, scale);
            _currentPath = opt.Data.Resource.CurrentSEPath;
            yield return new WaitForSeconds(3f);
        }
    }

    public void StopVFX()
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
        SEManager.Instance?.Stop(_currentPath);
    }
}