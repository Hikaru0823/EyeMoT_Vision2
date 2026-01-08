using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class ImageLoader
{
    // ----- Models -----

    [Serializable]
    public sealed class Library
    {
        public List<Entry> entries = new List<Entry>();
        public long createdUnixTime;
        public long updatedUnixTime;
    }

    [Serializable]
    public sealed class Entry
    {
        public string key;          // 表示・識別用（例: "A.png"）
        public string fileName;     // persistent側のファイル名
        public string cachedPath;   // persistent側フルパス
        public string originalPath; // 元フルパス（任意）
        public long addedUnixTime;
    }

    // ----- Fields -----

    private readonly string _sourceDir;   // exe同階層/YOUR_RESOURCES
    private readonly string _cacheDir;    // persistentDataPath/UserImages
    private readonly string _indexPath;   // persistentDataPath/image_library_index.json

    private readonly HashSet<string> _exts;

    private Library _library;

    // ----- Public -----

    public IReadOnlyList<Entry> Entries => _library.entries;

    public ImageLoader(
        string sourceDir,
        string cacheDir,
        string indexPath,
        IEnumerable<string> allowedExtensions = null)
    {
        _sourceDir = sourceDir ?? throw new ArgumentNullException(nameof(sourceDir));
        _cacheDir  = cacheDir  ?? throw new ArgumentNullException(nameof(cacheDir));
        _indexPath = indexPath ?? throw new ArgumentNullException(nameof(indexPath));

        _exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in (allowedExtensions ?? new[] { ".png", ".jpg", ".jpeg" }))
            _exts.Add(e.StartsWith(".") ? e : "." + e);

        _library = new Library { entries = new List<Entry>() };
    }

    /// <summary>
    /// 起動時に呼ぶ想定：
    /// 1) index読み込み（なければ作成）
    /// 2) YOUR_RESOURCES から persistentへ「未登録だけ」追加コピー
    /// ※削除同期はしない（sourceから消えてもpersistent/indexは残る）
    /// </summary>
    public void InitializeAndImportAddOnly()
    {
        EnsureDirs();
        LoadOrCreateIndex();
        ImportFromSource_AddOnly();
    }

    /// <summary>
    /// persistent側にコピー済みの指定画像を削除（indexからも削除）
    /// </summary>
    public bool RemoveByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        var entry = _library.entries
            .FirstOrDefault(e => string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase));

        if (entry == null) return false;

        // 1) cached file delete
        if (!string.IsNullOrEmpty(entry.cachedPath) && File.Exists(entry.cachedPath))
        {
            try { File.Delete(entry.cachedPath); }
            catch { return false; }
        }

        // 2) remove from index
        _library.entries.Remove(entry);

        TouchUpdated();
        SaveIndex();
        return true;
    }

    /// <summary>
    /// 永続データを全消し（index+cached files）
    /// </summary>
    public void ClearAll()
    {
        // delete files
        foreach (var e in _library.entries.ToList())
        {
            if (!string.IsNullOrEmpty(e.cachedPath) && File.Exists(e.cachedPath))
            {
                try { File.Delete(e.cachedPath); } catch { /* ignore */ }
            }
        }

        _library.entries.Clear();
        TouchUpdated();
        SaveIndex();
    }

    // ----- Internal -----

    private void EnsureDirs()
    {
        Directory.CreateDirectory(_sourceDir); // 無くても落ちないように
        Directory.CreateDirectory(_cacheDir);
    }

    private void LoadOrCreateIndex()
    {
        if (!File.Exists(_indexPath))
        {
            _library = new Library
            {
                entries = new List<Entry>(),
                createdUnixTime = NowUnix(),
                updatedUnixTime = NowUnix()
            };
            SaveIndex();
            return;
        }

        try
        {
            var json = File.ReadAllText(_indexPath);
            // Use Unity's JsonUtility for JSON serialization
            _library = UnityEngine.JsonUtility.FromJson<Library>(json)
                       ?? new Library { entries = new List<Entry>() };

            _library.entries ??= new List<Entry>();
        }
        catch
        {
            _library = new Library
            {
                entries = new List<Entry>(),
                createdUnixTime = NowUnix(),
                updatedUnixTime = NowUnix()
            };
            SaveIndex();
        }
    }

    private void SaveIndex()
    {
        var json = UnityEngine.JsonUtility.ToJson(_library, true);

        File.WriteAllText(_indexPath, json);
    }

    private void ImportFromSource_AddOnly()
    {
        foreach (var srcPath in Directory.EnumerateFiles(_sourceDir))
        {
            var ext = Path.GetExtension(srcPath);
            if (!_exts.Contains(ext)) continue;

            var key = Path.GetFileName(srcPath); // A.png など
            if (_library.entries.Any(e => string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase)))
                continue; // 既に登録済み

            // cache側のファイル名（基本は同名）
            var cachedFileName = key;
            var cachedPath = Path.Combine(_cacheDir, cachedFileName);

            // 同名衝突時はユニーク名にする
            if (File.Exists(cachedPath))
            {
                var name = Path.GetFileNameWithoutExtension(key);
                cachedFileName = $"{name}_{Guid.NewGuid():N}{ext}";
                cachedPath = Path.Combine(_cacheDir, cachedFileName);
            }

            File.Copy(srcPath, cachedPath, overwrite: false);

            _library.entries.Add(new Entry
            {
                key = key,
                fileName = cachedFileName,
                cachedPath = cachedPath,
                originalPath = srcPath,
                addedUnixTime = NowUnix()
            });

            TouchUpdated();
            SaveIndex();
        }
    }

    private void TouchUpdated() => _library.updatedUnixTime = NowUnix();
    private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
