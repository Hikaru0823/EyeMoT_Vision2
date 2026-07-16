using System.Collections;
using System.Collections.Generic;
using KanKikuchi.AudioManager;
using TMPro;
using UnityEngine;

public class RecordTimer : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] private TMP_Text _timerText;

    public void StartCountDown(System.Action onComplete = null)
    {
        StartCoroutine(CountDownCoroutine(onComplete));
    }

    private IEnumerator CountDownCoroutine(System.Action onComplete = null)
    {
        _timerText.enabled = true;
        float timer = 3f;
        var timerText = _timerText.text;
        while (timer > 0)
        {
            _timerText.text = Mathf.Ceil(timer).ToString();
            if(_timerText.text != timerText)
            {
                SEManager.Instance.Play(SEPath.HOVER);
            }
            yield return null;
            timer -= Time.deltaTime;
            timerText = _timerText.text;
        }
        onComplete?.Invoke();
        _timerText.text = "Start";
        
        yield return new WaitForSeconds(0.5f);
        _timerText.enabled = false;
    }
}
