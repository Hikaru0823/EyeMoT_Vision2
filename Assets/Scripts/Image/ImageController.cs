using KanKikuchi.AudioManager;
using System.Collections;
using UnityEngine;

public class ImageController : MonoBehaviour
{
    bool _AliveCheckEnable = false;
    float _AliveTimer = -1;
    Coroutine _PopCoroutine;
    Coroutine _DisappearCoroutine;
    bool _IsDisappearing = false;

    [SerializeField] float _PopStartScale = 0.8f;
    [SerializeField] float _PopOvershootScale = 1.08f;
    [SerializeField] float _PopInDuration = 0.12f;
    [SerializeField] float _PopOutDuration = 0.08f;
    [SerializeField] float _DisappearEndScale = 0.7f;
    [SerializeField] float _DisappearDuration = 0.12f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void init(float aliveTimer = -1)
    {
        _AliveTimer = aliveTimer;
        if (0 < aliveTimer)
        {
            _AliveCheckEnable = true;
        }

        playPopScale();
    }

    // Update is called once per frame
    void Update()
    {
        aliveCheck();
    }

    void aliveCheck()
    {
        if (!_AliveCheckEnable)
        {
            return;
        }

        _AliveTimer -= Time.deltaTime;
        if (_AliveTimer < 0 && !_IsDisappearing)
        {
            _AliveCheckEnable = false;
            playDisappearScale();
        }
    }

    void playPopScale()
    {
        if (_PopCoroutine != null)
        {
            StopCoroutine(_PopCoroutine);
        }
        _PopCoroutine = StartCoroutine(popScaleRoutine());
    }

    IEnumerator popScaleRoutine()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 startScale = baseScale * _PopStartScale;
        Vector3 overshootScale = baseScale * _PopOvershootScale;

        transform.localScale = startScale;

        float t = 0f;
        while (t < _PopInDuration)
        {
            t += Time.deltaTime;
            float ratio = _PopInDuration <= 0f ? 1f : Mathf.Clamp01(t / _PopInDuration);
            transform.localScale = Vector3.Lerp(startScale, overshootScale, ratio);
            yield return null;
        }

        t = 0f;
        while (t < _PopOutDuration)
        {
            t += Time.deltaTime;
            float ratio = _PopOutDuration <= 0f ? 1f : Mathf.Clamp01(t / _PopOutDuration);
            transform.localScale = Vector3.Lerp(overshootScale, baseScale, ratio);
            yield return null;
        }

        transform.localScale = baseScale;
        _PopCoroutine = null;
    }

    void playDisappearScale()
    {
        _IsDisappearing = true;
        if (_PopCoroutine != null)
        {
            StopCoroutine(_PopCoroutine);
        }
        if (_DisappearCoroutine != null)
        {
            StopCoroutine(_DisappearCoroutine);
        }
        _DisappearCoroutine = StartCoroutine(disappearScaleRoutine());
    }

    IEnumerator disappearScaleRoutine()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 endScale = baseScale * _DisappearEndScale;

        float t = 0f;
        while (t < _DisappearDuration)
        {
            t += Time.deltaTime;
            float ratio = _DisappearDuration <= 0f ? 1f : Mathf.Clamp01(t / _DisappearDuration);
            transform.localScale = Vector3.Lerp(baseScale, endScale, ratio);
            yield return null;
        }

        transform.localScale = endScale;
        Destroy(gameObject);
    }
}
