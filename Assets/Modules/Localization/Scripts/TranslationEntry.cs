using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Localization/Translation Entry", fileName = "TE_New")]
public class TranslationEntry : ScriptableObject
{
    [TextArea] public string Ja;
    [TextArea] public string En;

    // ★参照先（Editor専用の追跡情報）
#if UNITY_EDITOR
    [Serializable]
    public struct TextRef
    {
        public string scenePath;      // Assets/xxx.unity
        public string hierarchyPath;  // Canvas/Panel/Text
        public string componentType;  // Text or TMP_Text
    }

    public List<TextRef> refs = new List<TextRef>();
#endif
}
