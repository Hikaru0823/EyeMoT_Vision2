using System;
using System.Collections;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.UI;

public class ScreenCaptureDisplay : Singleton<ScreenCaptureDisplay>
{
    #region WinAPI Declaration
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);
    [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines, IntPtr bits, ref BITMAPINFO bmi, uint usage);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObj);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern bool TextOutW(IntPtr hdc, int x, int y, string lpString, int c);
    [DllImport("gdi32.dll")] static extern uint SetTextColor(IntPtr hdc, int crColor);
    [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr hdc, int iBkMode);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern bool GetTextExtentPoint32W(IntPtr hdc, string lpString, int c, out SIZE lpSize);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateFontIndirect([In] ref LOGFONT lplf);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct LOGFONT
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName;
    }
    [StructLayout(LayoutKind.Sequential)] struct SIZE { public int cx; public int cy; }

    const uint SRCCOPY = 0x00CC0020;

    // ── BITMAP 構造体 ───────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public uint bmiColors;   // 24 bit なので使わない
    }
    
    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    #endregion

    [SerializeField] Button captureButton;
    [SerializeField] Canvas[] hiddenCanvases;
    [Header("Capture Settings")]
    [SerializeField] string captureFolder = "YOUR_RECORD/OEKAKI";  // キャプチャ保存フォルダ名
    [SerializeField] string filePrefix = "EyeMoTCanvas_"; // ファイル名の接頭辞

    [Header("Date Settings")]
    [SerializeField] int dateFontSize = 86; // 日付フォントサイズ
    [SerializeField] FontFamily fontFamily = FontFamily.YuGothicUI; // 使用フォント
    public bool isIncludeDate = true;

    private string capturePath;

    void Start()
    {
        #if UNITY_WEBGL
        captureButton.interactable = false;
        #endif
        // キャプチャフォルダのパスを設定（.exeと同じ階層に作成）
        capturePath = Path.Combine(Application.dataPath, "..", captureFolder);
        //capturePath = Path.GetFullPath(capturePath); // 相対パスを絶対パスに変換
        
        // フォルダが存在しない場合は作成
        if (!Directory.Exists(capturePath))
        {
            Directory.CreateDirectory(capturePath);
            Debug.Log($"キャプチャフォルダを作成しました: {capturePath}");
        }
    }
    
    public void Capture()
    {
        #if !UNITY_WEBGL
        StartCoroutine(CaptureRoutine());
        #endif
    }

    IEnumerator CaptureRoutine()
    {
        //SEManager.Instance.Play(SEPath.CAMERA_SHUTTER);
        foreach (var canvas in hiddenCanvases)
        {
            canvas.enabled = false;
        }
        yield return new WaitForSeconds(0.1f);
        CaptureDesktop(captureFolder, filePrefix, System.DateTime.Now);
        yield return new WaitForSeconds(0.1f);
        foreach (var canvas in hiddenCanvases)
        {
            canvas.enabled = true;
        }

    }

    Texture2D CaptureDesktop(string Path, string prefix, System.DateTime date)
    {
        // Unity ウィンドウが属するモニターの矩形を取得
        IntPtr hWnd = GetActiveWindow();                                          // Unity 実行中のウィンドウ

        RECT winRect;
        if (!GetWindowRect(hWnd, out winRect))
        {
            Debug.LogError("GetWindowRect FAILED");
            return null;
        }

        // capture rectangle
        int left   = winRect.left;
        int top    = winRect.top;
        int width  = winRect.right  - winRect.left;
        int height = winRect.bottom - winRect.top;

        // 画面 → メモリ DC へコピー
        IntPtr hScreenDC = GetDC(IntPtr.Zero);
        IntPtr hMemDC = CreateCompatibleDC(hScreenDC);
        IntPtr hBitmap = CreateCompatibleBitmap(hScreenDC, width, height);
        IntPtr hOld = SelectObject(hMemDC, hBitmap);

        BitBlt(hMemDC, 0, 0, width, height, hScreenDC, left, top, SRCCOPY);

        // 日付描画
        if (isIncludeDate)
        {
            DrawDateText(date, hMemDC, width, height);
        }

        // 生ピクセルを取得
        int stride = width * 4;
        byte[] pixel = new byte[stride * height];

        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 /* BI_RGB */
            }
        };

        GCHandle hPin = GCHandle.Alloc(pixel, GCHandleType.Pinned);
        GetDIBits(hMemDC, hBitmap, 0, (uint)height, hPin.AddrOfPinnedObject(), ref bmi, 0);
        hPin.Free();
        // 日付テキストはBGR指定なので、初期値はA＝０になっている
        for (int i = 0; i < pixel.Length; i += 4)
        {
            // BGRA の A 部分
            pixel[i + 3] = 255;
        }


        // Unity Texture2D → PNG ファイル
        Texture2D tex = new Texture2D(width, height, TextureFormat.BGRA32, false);
        tex.LoadRawTextureData(pixel);
        tex.Apply();

        string path = Path + "/" + prefix + "_" + date.ToString("yyyyMMdd_HHmmss") + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log($"Saved desktop capture → {path}");

        // 5後片付け
        SelectObject(hMemDC, hOld);
        DeleteObject(hBitmap);
        DeleteDC(hMemDC);
        ReleaseDC(IntPtr.Zero, hScreenDC);

        return tex;
    }

    #region 日付描画
    public enum FontFamily
    {
        YuGothicUI,
        MSGothic,
        MSPGothic,
        Meiryo,
        MeiryoUI,
        Arial,
        TimesNewRoman,
        CourierNew,
        Verdana,
        Tahoma,
        TrebuchetMS,
        Georgia,
        Consolas,
        SegoeUI,
        Impact,
        ComicSansMS
    }
    string GetFontName(FontFamily font)
    {
        switch (font)
        {
            case FontFamily.YuGothicUI: return "Yu Gothic UI";
            case FontFamily.MSGothic: return "MS Gothic";
            case FontFamily.MSPGothic: return "MS PGothic";
            case FontFamily.Meiryo: return "Meiryo";
            case FontFamily.MeiryoUI: return "Meiryo UI";
            case FontFamily.Arial: return "Arial";
            case FontFamily.TimesNewRoman: return "Times New Roman";
            case FontFamily.CourierNew: return "Courier New";
            case FontFamily.Verdana: return "Verdana";
            case FontFamily.Tahoma: return "Tahoma";
            case FontFamily.TrebuchetMS: return "Trebuchet MS";
            case FontFamily.Georgia: return "Georgia";
            case FontFamily.Consolas: return "Consolas";
            case FontFamily.SegoeUI: return "Segoe UI";
            case FontFamily.Impact: return "Impact";
            case FontFamily.ComicSansMS: return "Comic Sans MS";
            default: return "Yu Gothic UI";
        }
    }

    void DrawDateText(System.DateTime date, IntPtr hMemDC, int width, int height)
    {
        string dateStr = date.ToString("yyyy/MM/dd  HH:mm");

        // フォント設定
        LOGFONT lf = new LOGFONT();
        lf.lfHeight = -dateFontSize;                 // ★ フォントの高さ（絶対値でサイズが大きくなる）
        lf.lfWeight = 700;                 // 太さ：700=Bold
        lf.lfFaceName = GetFontName(fontFamily);

        IntPtr hFont = CreateFontIndirect(ref lf);

        // このフォントに切り替え（戻り値 hOldFont を後で戻す）
        IntPtr hOldFont = SelectObject(hMemDC, hFont);

        // テキストサイズ取得
        SIZE textSize;
        GetTextExtentPoint32W(hMemDC, dateStr, dateStr.Length, out textSize);

        int margin = 16;
        int textX = width  - textSize.cx - margin;
        int textY = height - textSize.cy - margin;

        // 描画
        int outlineColor = 0x00000000; // 黒
        int fillColor    = 0x00FFFFFF; // 白
        DrawOutlinedText(hMemDC, dateStr, textX, textY, 2, outlineColor, fillColor);

        SelectObject(hMemDC, hOldFont);
        DeleteObject(hFont);
    }

    void DrawOutlinedText(
    IntPtr hdc,
    string text,
    int x,
    int y,
    int outlineSize,
    int outlineColor, // 0x00BBGGRR
    int fillColor     // 0x00BBGGRR
    )
    {
        const int TRANSPARENT = 1;
        SetBkMode(hdc, TRANSPARENT);

        // 1. アウトライン（周囲8方向＋αに描画）
        SetTextColor(hdc, outlineColor);

        for (int ox = -outlineSize; ox <= outlineSize; ox++)
        {
            for (int oy = -outlineSize; oy <= outlineSize; oy++)
            {
                // 中心は後で描くのでスキップ
                if (ox == 0 && oy == 0) continue;

                TextOutW(hdc, x + ox, y + oy, text, text.Length);
            }
        }

        // 2. 中心の文字（本体）
        SetTextColor(hdc, fillColor);
        TextOutW(hdc, x, y, text, text.Length);
    }

    #endregion
}

