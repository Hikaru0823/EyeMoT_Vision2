using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class MainSelecterUI : MonoBehaviour
{
    [Serializable]
    public class Selcter
    {
        public string Key;
        public Image Highlight;
    }

    [SerializeField] private Selcter[] _selecters;

    [SerializeField, ReadOnly] private string _currentKey = "";

    void Start()
    {
        OnSelect("None");
    }

    void OnEnable()
    {
        InterfaceManager.Instance?.SelectorPanelManager.OpenPanel(_currentKey + "Select");
    }

    public void OnSelect(string key)
    {
        if(_currentKey == key) return;

        foreach(var selcter in _selecters)
        {
            selcter.Highlight.enabled = (selcter.Key == key);
        }
        _currentKey = key;
    }

    public bool GetCurrentSelected(out ISendable sendableObj)
    {
        switch(_currentKey)
        {
            case "Image":
                sendableObj = ImageManager.Instance.GetCurrentImage();
                return true;
            case "VFX":
                sendableObj = VFXManager.Instance.GetCurrentVFX();
                return true;
            default:
                sendableObj = null;
                return false;
        }
    }
}