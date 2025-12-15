#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TranslationSelectAndAttachWindow : EditorWindow
{
    private const string PrefAuthKey = "TranslationSelectAndAttachWindow.DeepLAuthKey";

    [Serializable]
    class Item
    {
        public bool include = true;     // ★チェックボックス
        public GameObject go;           // 対象オブジェクト
        public string hierarchyPath;
        public string componentType;    // Text / TMP_Text
        public string ja;
        public string en;               // 翻訳結果 or Entryの既存En
        public TranslationEntry existingEntry; // 既存があれば
    }

    TranslationDb _db;
    DefaultAsset _entryFolder;
    string _authKey;
    string _sourceLang = "JA";
    string _targetLang = "EN";
    bool _translateOnlyIfEntryMissingOrEmpty = true; // ★差分判定（Entryベース）
    bool _sanitizeAssetName = true;

    Vector2 _scroll;
    string _status = "";

    readonly List<Item> _items = new();
    readonly Dictionary<string, string> _jaToEnCache = new(); // 同一JA翻訳キャッシュ

    [MenuItem("Tools/Localization/Select & Attach (SO Shared)")]
    public static void Open() => GetWindow<TranslationSelectAndAttachWindow>("Select & Attach");

    void OnEnable() => _authKey = EditorPrefs.GetString(PrefAuthKey, "");
    void OnDisable() => EditorPrefs.SetString(PrefAuthKey, _authKey ?? "");

    void OnGUI()
    {
        EditorGUILayout.LabelField("DB / Entry Folder", EditorStyles.boldLabel);
        _db = (TranslationDb)EditorGUILayout.ObjectField("TranslationDb", _db, typeof(TranslationDb), false);
        _entryFolder = (DefaultAsset)EditorGUILayout.ObjectField("Entry Folder", _entryFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("DeepL", EditorStyles.boldLabel);
        _authKey = EditorGUILayout.PasswordField("Auth Key", _authKey);
        _sourceLang = EditorGUILayout.TextField("Source", _sourceLang);
        _targetLang = EditorGUILayout.TextField("Target", _targetLang);

        EditorGUILayout.Space(6);
        _translateOnlyIfEntryMissingOrEmpty = EditorGUILayout.ToggleLeft(
            "Diff by Entry: translate only when Entry missing OR Entry.En empty",
            _translateOnlyIfEntryMissingOrEmpty);

        _sanitizeAssetName = EditorGUILayout.ToggleLeft("Sanitize Entry asset name", _sanitizeAssetName);

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Translation（一覧作成＆差分翻訳）", GUILayout.Height(30)))
                _ = BuildListAndTranslateAsync();

            if (GUILayout.Button("Include: All", GUILayout.Height(30), GUILayout.Width(110)))
                foreach (var it in _items) it.include = true;

            if (GUILayout.Button("Include: None", GUILayout.Height(30), GUILayout.Width(110)))
                foreach (var it in _items) it.include = false;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply（チェックONだけ Entry生成＆付与）", GUILayout.Height(30)))
                ApplySelected();
            if (GUILayout.Button("Apply All Texts Now", GUILayout.Height(30), GUILayout.Width(160)))
                LocalizationManager.ApplyAll();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(string.IsNullOrEmpty(_status) ? "Ready." : _status, MessageType.Info);

        DrawList();
    }

    void DrawList()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Use", GUILayout.Width(40));
            GUILayout.Label("Object", GUILayout.Width(220));
            GUILayout.Label("Type", GUILayout.Width(70));
            GUILayout.Label("JA", GUILayout.Width(320));
            GUILayout.Label("EN (translated / existing)", GUILayout.Width(360));
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var it in _items)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                it.include = EditorGUILayout.Toggle(it.include, GUILayout.Width(40));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(it.go, typeof(GameObject), true, GUILayout.Width(220));
                    EditorGUILayout.TextField(it.componentType, GUILayout.Width(70));
                }

                it.ja = EditorGUILayout.TextField(it.ja, GUILayout.Width(320));
                //最初の位置文字目を大文字にしたい
                string displayEn = CapitalizeFirst(it.en);
                string newEn = EditorGUILayout.TextField(displayEn, GUILayout.Width(360));
                it.en = newEn;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    async Task BuildListAndTranslateAsync()
    {
        if (_db == null) { _status = "TranslationDb is not set."; Repaint(); return; }
        if (string.IsNullOrWhiteSpace(_authKey)) { _status = "Auth Key is empty."; Repaint(); return; }

        _db.BuildIndex();
        _items.Clear();

        var scene = SceneManager.GetActiveScene();
        var scenePath = scene.path; // Assets/xxx.unity

        // 1) 全Text/TMP_Textを列挙
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                // Text
                var uiText = tr.GetComponent<Text>();
                if (uiText != null && !string.IsNullOrWhiteSpace(uiText.text))
                    AddItem(tr.gameObject, scenePath, GetHierarchyPath(tr), "Text", uiText.text);

                // TMP_Text
                var tmp = tr.GetComponent<TMP_Text>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                    AddItem(tr.gameObject, scenePath, GetHierarchyPath(tr), "TMP_Text", tmp.text);
            }
        }

        if (_items.Count == 0)
        {
            _status = "No Text/TMP_Text found in active scene.";
            Repaint();
            return;
        }

        // 2) Entryベース差分：翻訳が必要なJAを抽出（同一JAは1回だけ）
        var needJa = new HashSet<string>();
        foreach (var it in _items)
        {
            // 既存Entryがあれば拾う
            if (_db.TryGetByJa(it.ja, out var entry) && entry != null)
            {
                it.existingEntry = entry;
                it.en = entry.En; // 既存Enをまず表示

                if (_translateOnlyIfEntryMissingOrEmpty)
                {
                    if (string.IsNullOrWhiteSpace(entry.En)) needJa.Add(it.ja);
                }
                else
                {
                    needJa.Add(it.ja);
                }
            }
            else
            {
                it.existingEntry = null;
                // Entryが無いなら翻訳候補
                needJa.Add(it.ja);
            }
        }

        // キャッシュ済みは除外
        var toTranslate = needJa.Where(ja => !_jaToEnCache.ContainsKey(ja)).ToList();

        _status = $"Found {_items.Count} components. Translating {toTranslate.Count} unique texts...";
        Repaint();

        // 3) DeepL翻訳
        const int batchSize = 30;
        for (int i = 0; i < toTranslate.Count; i += batchSize)
        {
            var batch = toTranslate.Skip(i).Take(batchSize).ToList();
            var translated = await DeepLTranslateAsync(_authKey, batch, _sourceLang, _targetLang);
            if (translated == null || translated.Count != batch.Count)
            {
                _status = "Translation failed. Check Console.";
                Repaint();
                return;
            }
            for (int j = 0; j < batch.Count; j++)
                _jaToEnCache[batch[j]] = translated[j];
        }

        // 4) 一覧へ翻訳結果を反映（Entryが空なら翻訳を表示）
        foreach (var it in _items)
        {
            if (_jaToEnCache.TryGetValue(it.ja, out var trEn))
            {
                // 既存EntryがありEnが埋まってるなら、そのまま維持
                if (it.existingEntry != null && !string.IsNullOrWhiteSpace(it.existingEntry.En))
                    continue;

                it.en = trEn;
            }
        }

        _status = "List ready. Toggle include checkboxes, then Apply.";
        Repaint();
    }

    void AddItem(GameObject go, string scenePath, string hierarchyPath, string type, string ja)
    {
        _items.Add(new Item
        {
            include = true,
            go = go,
            hierarchyPath = hierarchyPath,
            componentType = type,
            ja = ja,
            en = "",
            existingEntry = null
        });
    }

    void ApplySelected()
    {
        if (_db == null) { _status = "TranslationDb is not set."; return; }
        if (_entryFolder == null) { _status = "Entry Folder is not set."; return; }

        string folderPath = AssetDatabase.GetAssetPath(_entryFolder);
        if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith("Assets"))
        {
            _status = "Entry Folder must be under Assets/.";
            return;
        }

        _db.BuildIndex();

        var scene = SceneManager.GetActiveScene();
        var scenePath = scene.path;

        // ★同一JAは同じEntryを共有（今回選択分）
        var createdOrUsed = new Dictionary<string, TranslationEntry>();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        int attached = 0, created = 0, linked = 0, refsAdded = 0;

        foreach (var it in _items.Where(x => x.include))
        {
            if (string.IsNullOrWhiteSpace(it.ja)) continue;

            // 1) Entryを取得（既存 or 今回作成/共有）
            if (!createdOrUsed.TryGetValue(it.ja, out var entry) || entry == null)
            {
                if (_db.TryGetByJa(it.ja, out var existing) && existing != null)
                {
                    entry = existing;
                }
                else
                {
                    entry = CreateEntryAsset(folderPath, it.ja, it.en);
                    _db.Add(entry);
                    created++;
                }

                // Enが空なら一覧のENを反映（あなたの一覧手編集も反映できる）
                if (string.IsNullOrWhiteSpace(entry.En) && !string.IsNullOrWhiteSpace(it.en))
                {
                    Undo.RecordObject(entry, "Set Entry En");
                    entry.En = it.en;
                    EditorUtility.SetDirty(entry);
                }

                createdOrUsed[it.ja] = entry;
                createdOrUsed[it.ja] = entry;
                linked++;
            }

            // 2) TranslationText を付与してEntry参照をセット
            var tt = it.go.GetComponent<TranslationText>();
            if (tt == null)
            {
                tt = Undo.AddComponent<TranslationText>(it.go);
                attached++;
            }

            Undo.RecordObject(tt, "Link Translation Entry");
            tt.entry = entry;
            EditorUtility.SetDirty(tt);

            // 3) Entryに参照先（ScenePath/Hierarchy/Type）を保存（Editor専用）
#if UNITY_EDITOR
            Undo.RecordObject(entry, "Add Entry Ref");
            entry.refs ??= new List<TranslationEntry.TextRef>();
            entry.refs.Add(new TranslationEntry.TextRef
            {
                scenePath = scenePath,
                hierarchyPath = it.hierarchyPath,
                componentType = it.componentType
            });
            EditorUtility.SetDirty(entry);
            refsAdded++;
#endif
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(_db);
        AssetDatabase.SaveAssets();

        _status = $"Apply done. Created:{created} Attached:{attached} Linked:{linked} RefsAdded:{refsAdded}";
        Repaint();
    }

    static TranslationEntry CreateEntryAsset(string folderPath, string ja, string en)
    {
        var e = ScriptableObject.CreateInstance<TranslationEntry>();
        e.Ja = ja;
        e.En = en ?? "";

        string name = MakeSafeAssetName(ja);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/TE_{name}.asset");
        AssetDatabase.CreateAsset(e, path);
        EditorUtility.SetDirty(e);
        return e;
    }

    static string MakeSafeAssetName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "EMPTY";
        s = s.Trim();
        var arr = s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        var t = new string(arr);
        while (t.Contains("__")) t = t.Replace("__", "_");
        t = t.Trim('_');
        if (t.Length > 24) t = t.Substring(0, 24);
        return string.IsNullOrEmpty(t) ? "TEXT" : t;
    }

    static string GetHierarchyPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }

    // ---- DeepL (403対策：x-www-form-urlencoded + Free/Pro自動)
    async Task<List<string>> DeepLTranslateAsync(string authKey, List<string> texts, string sourceLang, string targetLang)
    {
        authKey = (authKey ?? "").Trim();

        string baseUrl = authKey.EndsWith(":fx", StringComparison.OrdinalIgnoreCase)
            ? "https://api-free.deepl.com"
            : "https://api.deepl.com";

        string url = baseUrl + "/v2/translate";

        var pairs = new List<string>
        {
            "auth_key=" + Uri.EscapeDataString(authKey),
            "source_lang=" + Uri.EscapeDataString(sourceLang),
            "target_lang=" + Uri.EscapeDataString(targetLang),
        };
        foreach (var t in texts)
            pairs.Add("text=" + Uri.EscapeDataString(t));

        string body = string.Join("&", pairs);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=utf-8");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Delay(10);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DeepL error: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
            return null;
        }

        return ExtractTranslationTexts(req.downloadHandler.text);
    }

    static List<string> ExtractTranslationTexts(string json)
    {
        var results = new List<string>();
        int idx = 0;
        while (true)
        {
            idx = json.IndexOf("\"text\":", idx, StringComparison.Ordinal);
            if (idx < 0) break;
            idx = json.IndexOf('"', idx + 7);
            if (idx < 0) break;
            int end = json.IndexOf('"', idx + 1);
            if (end < 0) break;

            var raw = json.Substring(idx + 1, end - (idx + 1));
            results.Add(UnescapeJsonString(raw));
            idx = end + 1;
        }
        return results;
    }

    static string UnescapeJsonString(string s)
    {
        return s.Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
    }

    static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.Length == 1) return input.ToUpper();
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
#endif
