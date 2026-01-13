using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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
    private readonly long _maxFileSizeBytes;

    private Library _library;

    // ----- Public -----

    public IReadOnlyList<Entry> Entries => _library.entries;

    public ImageLoader(
        string sourceDir,
        string cacheDir,
        string indexPath,
        long maxFileSizeBytes = 0,
        IEnumerable<string> allowedExtensions = null)
    {
        _sourceDir = sourceDir ?? throw new ArgumentNullException(nameof(sourceDir));
        _cacheDir  = cacheDir  ?? throw new ArgumentNullException(nameof(cacheDir));
        _indexPath = indexPath ?? throw new ArgumentNullException(nameof(indexPath));

        _exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in (allowedExtensions ?? new[] { ".png", ".jpg", ".jpeg" }))
            _exts.Add(e.StartsWith(".") ? e : "." + e);

        _maxFileSizeBytes = maxFileSizeBytes;
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

            if (_maxFileSizeBytes > 0 && new FileInfo(srcPath).Length > _maxFileSizeBytes)
            {
                if (!TryResizeAndSave(srcPath, cachedPath, ext))
                    File.Copy(srcPath, cachedPath, overwrite: false);
            }
            else
            {
                File.Copy(srcPath, cachedPath, overwrite: false);
            }

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

    private bool TryResizeAndSave(string srcPath, string dstPath, string ext)
    {
        Texture2D original = null;
        Texture2D working = null;

        try
        {
            var bytes = File.ReadAllBytes(srcPath);
            original = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!original.LoadImage(bytes))
                return false;

            working = original;
            var encoded = EncodeTexture(working, ext);

            int width = working.width;
            int height = working.height;
            var resizedOnce = false;

            while ((encoded.Length > _maxFileSizeBytes || !resizedOnce) && width > 1 && height > 1)
            {
                var basisSize = resizedOnce ? encoded.Length : bytes.Length;
                var ratio = Math.Sqrt((double)_maxFileSizeBytes / basisSize);
                if (double.IsNaN(ratio) || ratio <= 0)
                    ratio = 0.5;

                var newWidth = Math.Max(1, (int)Math.Floor(width * ratio));
                var newHeight = Math.Max(1, (int)Math.Floor(height * ratio));
                if (newWidth == width && newHeight == height)
                {
                    newWidth = Math.Max(1, width - 1);
                    newHeight = Math.Max(1, height - 1);
                }

                var resized = ResizeTexture(working, newWidth, newHeight);
                if (working != original)
                    UnityEngine.Object.Destroy(working);

                working = resized;
                width = newWidth;
                height = newHeight;
                resizedOnce = true;
                encoded = EncodeTexture(working, ext);
            }

            File.WriteAllBytes(dstPath, encoded);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (working != null && working != original)
                UnityEngine.Object.Destroy(working);
            if (original != null)
                UnityEngine.Object.Destroy(original);
        }
    }

    private static byte[] EncodeTexture(Texture2D tex, string ext)
    {
        if (string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return tex.EncodeToJPG(90);
        }

        return tex.EncodeToPNG();
    }

    private static Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;

        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        var result = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private void TouchUpdated() => _library.updatedUnixTime = NowUnix();
    private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
