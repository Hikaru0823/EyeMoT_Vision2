using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TranslationText : MonoBehaviour
{
    [SerializeField] bool _translate = true;
    public TranslationEntry entry;

    Text _uiText;
    TMP_Text _tmp;

    void Awake()
    {
        _uiText = GetComponent<Text>();
        _tmp = GetComponent<TMP_Text>();
        Apply();
    }

    public void Apply()
    {
        if (entry == null || !_translate) return;
        var s = LocalizationManager.CurrentLanguage == Language.JA ? entry.Ja : entry.En;

        if (_uiText != null) _uiText.text = s;
        if (_tmp != null) _tmp.text = s;
    }
}