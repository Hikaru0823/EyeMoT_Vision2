using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Localization/Translation DB", fileName = "TranslationDb")]
public class TranslationDb : ScriptableObject
{
    public List<TranslationEntry> entries = new List<TranslationEntry>();

    Dictionary<string, TranslationEntry> _byJa;

    public void BuildIndex()
    {
        _byJa = new Dictionary<string, TranslationEntry>();
        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.Ja)) continue;
            if (!_byJa.ContainsKey(e.Ja)) _byJa.Add(e.Ja, e);
        }
    }

    public bool TryGetByJa(string ja, out TranslationEntry entry)
    {
        if (_byJa == null) BuildIndex();
        return _byJa.TryGetValue(ja, out entry);
    }

    public void Add(TranslationEntry entry)
    {
        if (entry == null) return;
        if (!entries.Contains(entry)) entries.Add(entry);
    }
}
