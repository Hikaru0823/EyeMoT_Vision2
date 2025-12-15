using UnityEngine;
using System.IO;
using UnityEngine.UI;
using KanKikuchi.AudioManager;

public class ScreenCapture : MonoBehaviour
{
    [Header("Capture Settings")]
    public string captureFolder = "Captures";  // キャプチャ保存フォルダ名
    public string filePrefix = "EyeMoTCanvas_"; // ファイル名の接頭辞
    public ScreenCaptureDisplay screenCaptureDisplay = new ScreenCaptureDisplay();
    
    private string capturePath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // キャプチャフォルダのパスを設定（.exeと同じ階層に作成）
        capturePath = Path.Combine(Application.dataPath, "..", captureFolder);
        capturePath = Path.GetFullPath(capturePath); // 相対パスを絶対パスに変換
        
        // フォルダが存在しない場合は作成
        if (!Directory.Exists(capturePath))
        {
            Directory.CreateDirectory(capturePath);
            Debug.Log($"キャプチャフォルダを作成しました: {capturePath}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Cキーが押されたらキャプチャを保存
        if (Input.GetKeyDown(KeyCode.C))
        {
            ScreenShot();
        }
    }

    public void ScreenShot()
    {
        #if !UNITY_WEBGL
        //screenCaptureDisplay.CaptureDesktop(capturePath, filePrefix, System.DateTime.Now);
        #endif
    }

    void CaptureScreen(string fileName = null)
    {
        // ファイル名が指定されていない場合は自動生成
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"{filePrefix}Texture_{timestamp}";
        }
        string fullPath = Path.Combine(capturePath, fileName);

        // スクリーンキャプチャを保存
        UnityEngine.ScreenCapture.CaptureScreenshot(fullPath);
        
        Debug.Log($"スクリーンキャプチャを保存しました: {fullPath}");
    }

    /// <summary>
    /// 指定されたTextureをPNG形式で保存する
    /// </summary>
    /// <param name="texture">保存するTexture</param>
    /// <param name="fileName">ファイル名（拡張子なし）。nullの場合は自動生成</param>
    /// <returns>保存されたファイルのフルパス</returns>
    public string SaveTextureToPng(Texture2D texture, string fileName = null)
    {
        if (texture == null)
        {
            Debug.LogError("保存するTextureがnullです");
            return null;
        }

        // ファイル名が指定されていない場合は自動生成
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"{filePrefix}_{timestamp}";
        }

        string fullPath = Path.Combine(capturePath, fileName + ".png");

        // TextureをPNGバイト配列に変換
        byte[] pngData = texture.EncodeToPNG();
        
        if (pngData != null)
        {
            // ファイルに書き込み
            File.WriteAllBytes(fullPath, pngData);
            Debug.Log($"Textureを保存しました: {fullPath}");
            return fullPath;
        }
        else
        {
            Debug.LogError("TextureのPNG変換に失敗しました");
            return null;
        }
    }

    /// <summary>
    /// RenderTextureをTexture2Dに変換してPNG形式で保存する
    /// </summary>
    /// <param name="renderTexture">保存するRenderTexture</param>
    /// <param name="fileName">ファイル名（拡張子なし）。nullの場合は自動生成</param>
    /// <returns>保存されたファイルのフルパス</returns>
    public string SaveRenderTextureToPng(RenderTexture renderTexture, string fileName = null)
    {
        if (renderTexture == null)
        {
            Debug.LogError("保存するRenderTextureがnullです");
            return null;
        }

        // RenderTextureをTexture2Dに変換
        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentActiveRT;

        // Texture2DをPNGで保存
        string result = SaveTextureToPng(texture2D, fileName);

        // 一時的に作成したTexture2Dを破棄
        DestroyImmediate(texture2D);

        return result;
    }

    public string SaveRawImageToPng(RawImage rawImage, string fileName = null)
    {
        //SEManager.Instance.Play(SEPath.SHUTTER);
        if (rawImage == null)
        {
            Debug.LogError("保存するRawImageがnullです");
            return null;
        }

        // 元のテクスチャ（たとえばrtA/rtBのどちらか）
        var srcTex = rawImage.texture;
        var mat = rawImage.material;

        // 一時的にBlit結果を書き出すRenderTexture
        var tempRT = new RenderTexture(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(srcTex, tempRT, mat);

        // テクスチャ→PNG
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tempRT;
        Texture2D tex = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        // Texture2DをPNGで保存
        string result = SaveTextureToPng(tex, fileName);

        Destroy(tex);
        tempRT.Release();

        return result;
    }
}
