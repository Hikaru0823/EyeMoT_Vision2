using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSelecterUI : SimpleHighlightSelecterOptionUI
{
    [SerializeField] private GameObject _imageButtonPrefab;
    [SerializeField] private Transform _contentParent;
    [SerializeField] private Image _iconImage;
    [SerializeField] private ImageOption[] _ImageOptions;
    [SerializeField] private UnityEngine.Events.UnityEvent<string> onImageChanged;

    void Start()
    {
        if(ImageManager.Instance.TryGetAll(out var imgDict))
        {
            _ImageOptions = new ImageOption[imgDict.Count];
            int index = 0;
            foreach(var kvp in imgDict) 
            {
                var button = Instantiate(_imageButtonPrefab, _contentParent);
                var iconImage = button.transform.Find("Icon").GetComponent<Image>();
                iconImage.sprite = Sprite.Create(
                    kvp.Value,
                    new Rect(0, 0, kvp.Value.width, kvp.Value.height),
                    new Vector2(0.5f, 0.5f)
                );
                iconImage.preserveAspect = true;
                var option = new ImageOption()
                {
                    Key = kvp.Key,
                    Button = button.GetComponent<Button>(),
                };
                _ImageOptions[index] = option;
                index++;
            }
            AutoAssign(_ImageOptions);
            Init(_ImageOptions);
        }
    }

    // void OnEnable()
    // {
    //     // 保存されているVFXを読み込み、選択状態にする
    //     OnSelected();
    // }

    public void OnSelected()
    {
        var savedImageKey = ES3.Load<string>(SaveKey.IMAGE_SELECTED, defaultValue: null);
        if(TryGetOption(savedImageKey, out var option))
        {
            OnOptionSelected(option);
        }
    }

    bool TryGetOption(string key, out ImageOption option)
    {
        foreach(var opt in _ImageOptions)
        {
            if(opt.Key == key)
            {
                option = opt;
                return true;
            }
        }
        option = null;
        return false;
    }

    public override void OnOptionSelected(OptionButtonResources option)
    {
        base.OnOptionSelected(option);
        if(ImageManager.Instance.TryGet((option as ImageOption).Key, out var texture))
        {
            ImageManager.Instance.SetCurrentImage((option as ImageOption).Key);
            ES3.Save<string>(SaveKey.IMAGE_SELECTED, (option as ImageOption).Key);
            _iconImage.sprite = option.PreviewImage.sprite;
            onImageChanged?.Invoke("Image");
        }
    }
}

[Serializable]
public class ImageOption : OptionButtonResources
{
    public string Key;
}