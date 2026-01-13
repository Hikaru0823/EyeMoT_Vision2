using System.Collections;
using System.Collections.Generic;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private Image seCover;
    [SerializeField] private Image bgmCover;

    [SerializeField, ReadOnly] private SoundState _state = SoundState.ALL_ON;
    [SerializeField, ReadOnly] private float _seVolume = 0.3f;
    [SerializeField, ReadOnly] private float _bgmVolume = 0.3f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        _state = ES3.Load<SoundState>(SaveKey.SOUND_STATE, defaultValue:SoundState.ALL_ON);
        _seVolume = ES3.Load<float>(SaveKey.SOUND_SE_VOLUME, defaultValue:0.3f);
        _bgmVolume = ES3.Load<float>(SaveKey.SOUND_BGM_VOLUME, defaultValue:0.3f);
        UpdateState();
    }

    public void ChangeState(SoundState state)
    {
        _state = state;
        UpdateState();
    }

    // UI hook
    public void ChangeState()
    {
        _state = (SoundState)(((int)_state + 1) % 4);
        UpdateState();
    }

    private void UpdateState()
    {
        switch (_state)
        {
            case SoundState.ALL_ON:
                seCover.enabled = false;
                bgmCover.enabled = false;
                SEManager.Instance.ChangeBaseVolume(_seVolume);
                BGMManager.Instance.ChangeBaseVolume(_bgmVolume);
                break;
            case SoundState.SE_OFF:
                seCover.enabled = true;
                bgmCover.enabled = false;
                SEManager.Instance.ChangeBaseVolume(0);
                BGMManager.Instance.ChangeBaseVolume(_bgmVolume);
                break;
            case SoundState.BGM_OFF:
                seCover.enabled = false;
                bgmCover.enabled = true;
                SEManager.Instance.ChangeBaseVolume(_seVolume);
                BGMManager.Instance.ChangeBaseVolume(0);
                break;
            case SoundState.ALL_OFF:
                seCover.enabled = true;
                bgmCover.enabled = true;
                SEManager.Instance.ChangeBaseVolume(0);
                BGMManager.Instance.ChangeBaseVolume(0);
                break;
        }

        ES3.Save<SoundState>(SaveKey.SOUND_STATE, _state);
    }

    public void ChangeSEVolume(float volume)
    {
        _seVolume = volume;
        ES3.Save<float>(SaveKey.SOUND_SE_VOLUME, _seVolume);
        SEManager.Instance.ChangeBaseVolume(volume);
    }

    public void ChangeBGMVolume(float volume)
    {
        _bgmVolume = volume;
        ES3.Save<float>(SaveKey.SOUND_BGM_VOLUME, _bgmVolume);
        BGMManager.Instance.ChangeBaseVolume(volume);
    }

    public enum SoundState
    {
        ALL_ON = 0,
        SE_OFF,
        BGM_OFF,
        ALL_OFF
    }
}
