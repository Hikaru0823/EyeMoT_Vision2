using System.Collections;
using System.Collections.Generic;
using EyeMoT.GameRecoder;
using EyeMoT.Heatmap;
using UnityEngine;
using UnityEngine.UI;

public class RecordManager : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] private Sprite[] _recordIcon; // 0: Record, 1: Stop
    [SerializeField] private Image _recordButtonImage;
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

    public void OnHideHeatmapButtonPressed()
    {
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
        InterfaceManager.Instance.RecordTimer.StartCountDown(() =>
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            GameRecoder.Instance.RecordStart();
            #endif
            HeatmapRenderer.Instance.StartHeatmap(isDynamicDraw: true);
            OnHideHeatmapButtonPressed();
        });

        if(ClientManager.Instance == null) return;
        ClientManager.Instance.SendTcp(
        NetJson.ToJson(new NetMessage<ChatPayload>
        {
            Type = NetMessageType.RecordStart,
            SenderId = ClientManager.Instance.Idx,
            TargetId = 2, // clientへ
            Payload = new ChatPayload { Text = "" }
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
