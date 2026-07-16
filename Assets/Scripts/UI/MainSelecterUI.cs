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
    [SerializeField, ReadOnly] private string _currentSelecter = "";

    void Start()
    {
        foreach(var selcter in _selecters)
        {
            selcter.Highlight.enabled = false;
        }
    }

    // void OnEnable()
    // {
    //     InterfaceManager.Instance?.SelectorPanelManager.OpenPanel(_currentKey + "Select");
    // }

    public void OnSelect(string key)
    {
        //Debug.Log("MainSelecterUI OnSelect: " + key + " CurrentKey: " + _currentKey);
        if(_currentKey == key)
        {
            // foreach(var selcter in _selecters)
            // {
            //     selcter.Highlight.enabled = false;
            // }
            _currentKey = "";  
            InterfaceManager.Instance?.SelectorPanelManager.OpenPanel("Null");
            return;
        }

        foreach(var selcter in _selecters)
        {
            selcter.Highlight.enabled = (selcter.Key == key);
        }
        _currentKey = key;
        _currentSelecter = key;
        InterfaceManager.Instance?.SelectorPanelManager.OpenPanel(_currentKey + "Select");
    }

    public bool GetCurrentSelected(out ISendable sendableObj)
    {
        switch(_currentSelecter)
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