using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ImageManager : MonoBehaviour
{
    public static ImageManager Instance { get; private set; }
    [Header("exeと同じ階層の読み込み元フォルダ名")]
    [SerializeField] private string sourceFolderName = "YOUR_RESOURCES";

    [Header("persistentDataPath側に作るフォルダ名")]
    [SerializeField] private string cacheFolderName = "UserImages";

    [Header("インポート時に許可する最大ファイルサイズ(バイト)")]
    [SerializeField] private long maxImportFileSizeBytes = 3 * 1024 * 1024;

    private ImageLoader Library { get; set; }
    [SerializeField, ReadOnly] Dictionary<string, Texture2D> _textures = new();
    [SerializeField, ReadOnly] string _currentImageKey = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        var exeDir = GetExeDir();
        var sourceDir = Path.Combine(exeDir, sourceFolderName);
        var cacheDir  = Path.Combine(Application.persistentDataPath, cacheFolderName);
        var indexPath = Path.Combine(Application.persistentDataPath, "image_library_index.json");

        Library = new ImageLoader(sourceDir, cacheDir, indexPath, maxImportFileSizeBytes);
        Library.InitializeAndImportAddOnly();

        Debug.Log($"[Library] Count = {Library.Entries.Count}");
        foreach (var e in Library.Entries)
            Debug.Log($"[Library] {e.key} (cached: {File.Exists(e.cachedPath)})");
        LoadTexturesFromCache();
    }

    public bool Remove(string key)
    {
        // Unity依存はここでは不要。純C#側が削除まで行う
        return Library.RemoveByKey(key);
    }

    private string GetExeDir()
    {
        // Windowsビルド：Application.dataPath = ".../MyGame_Data"
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    private void LoadTexturesFromCache()
    {
        ClearTextures();

        foreach (var e in Library.Entries)
        {
            if (!File.Exists(e.cachedPath)) continue;
            if (TryLoadTexture(e.cachedPath, out var tex))
                _textures[e.key] = tex; // keyで参照
        }
    }

    private bool TryLoadTexture(string path, out Texture2D tex)
    {
        tex = null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!tex.LoadImage(bytes))
            {
                Destroy(tex);
                tex = null;
                return false;
            }
            return true;
        }
        catch
        {
            if (tex != null) Destroy(tex);
            tex = null;
            return false;
        }
    }

    public bool TryGet(string key, out Texture2D tex)
    {
        return _textures.TryGetValue(key, out tex);
    }

    public bool TryGetAll(out Dictionary<string, Texture2D> textures)
    {
        textures = new Dictionary<string, Texture2D>();
        foreach (var e in Library.Entries)
        {
            if (TryGet(e.key, out var tex))
                textures.Add(e.key, tex);
        }
        return textures.Count > 0;
    }

    public void SetCurrentImage(string key)
    {
        _currentImageKey = key;
    }

    private void ClearTextures()
    {
        foreach (var kv in _textures)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        _textures.Clear();
    }
}
