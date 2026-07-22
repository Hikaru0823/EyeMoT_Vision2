using System.Collections;
using System.Collections.Generic;
using EyeMoT.GameRecoder;
using EyeMoT.Heatmap;
using UnityEngine;
using UnityEngine.UI;

public class RecordManager : MonoBehaviour
{
    public static RecordManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    [Header("Resources")]
    [SerializeField] private Sprite[] _recordIcon; // 0: Record, 1: Stop
    [SerializeField] private Image _recordButtonImage;
    [SerializeField] private Button _recordButton;
    [SerializeField] private Button _hideHeatmapButton;
    private bool isRecording = false;

    void Start()
    {
        // 録画アイコンをRecordに設定
        UpdateRecordIcon();
    }

    private void UpdateRecordIcon()
    {
        if(_recordButtonImage != null)
        {
            _recordButtonImage.sprite = isRecording ? _recordIcon[1] : _recordIcon[0];
        }
    }

    public void Init()
    {
        HeatmapRenderer.Instance.StopHeatmap(false);
        #if !UNITY_WEBGL || UNITY_EDITOR
        GameRecoder.Instance.RecordEnd();
        #endif
        _recordButtonImage.sprite = _recordIcon[0];
        OnHideHeatmapButtonPressed();
    }

    public void OnHideHeatmapButtonPressed()
    {
        HeatmapRenderer.Instance.ClearHeatmap();
        HeatmapRenderer.Instance.VisibleHeatmap(false);
        _hideHeatmapButton.gameObject.SetActive(false);
    }

    public void OnRecordButtonPressed()
    {
        if(isRecording)
        {
            RecordEnd();
        }
        else
        {
            RecordStart();
        }
        isRecording = !isRecording;
        UpdateRecordIcon();
    }

    public void RecordStart()
    {
        HeatmapRenderer.Instance.ClearHeatmap();
        _recordButton.interactable = false;
        InterfaceManager.Instance.RecordTimer.StartCountDown(() =>
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            GameRecoder.Instance.RecordStart();
            #endif
            _recordButton.interactable = true;
            HeatmapRenderer.Instance.StartHeatmap(isDynamicDraw: true);
            OnHideHeatmapButtonPressed();
        });

        NetworkBootStrap.Instance.ServerManager.SendTcp(
        NetJson.ToJson(new NetMessage<StringPayload>
        {
            Type = NetMessageType.RecordStart,
            SenderId = -1,
            TargetId = -2, // client全員へ
            Payload = new StringPayload { Text = "" }
        }));
    }

    public void RecordEnd()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        GameRecoder.Instance.RecordEnd();
        #endif
        HeatmapRenderer.Instance.StopHeatmap();
        HeatmapRenderer.Instance.VisibleHeatmap(true);
        _hideHeatmapButton.gameObject.SetActive(true);
    }
}
