using UnityEngine;
public enum Language { JA, EN }

public static class LocalizationManager
{
    public static Language CurrentLanguage = Language.JA;

    public static void ApplyAll(Language lang = Language.JA)
    {
        CurrentLanguage = lang;
        foreach (var t in Object.FindObjectsOfType<TranslationText>(true))
            t.Apply();
    }
}