using KanKikuchi.AudioManager;
using UnityEngine;

public class VFXController : MonoBehaviour
{
    bool _AliveCheckEnable = false;
    float _AliveTimer = -1;
    ParticleSystem _ParticleSystem = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void init(VFXResource data, bool canPlaySE, bool canPlayLowSE, float aliveTimer = -1, bool isSpatial  =false)
    {
        _ParticleSystem = GetComponent<ParticleSystem>();
        if (_ParticleSystem == null)
        {
            Debug.LogError($"ParticleSystem not found on {this.gameObject.name}");
        }

        if (canPlaySE)
        {
            if (isSpatial)
                SEManager.Instance.PlayAtPoint(data.CurrentSEPath, transform.position);
            else
                SEManager.Instance.Play(data.CurrentSEPath);
        }
        if (canPlayLowSE)
        {
            if(isSpatial)
                SEManager.Instance.PlayAtPoint(SEPath.LOW50_HZ, transform.position);
            else
                SEManager.Instance.Play(SEPath.LOW50_HZ);
        }

        _AliveTimer = aliveTimer;
        if (0 < aliveTimer)
        {
            playStart();
            _AliveCheckEnable = true;
        }
        else
        {
            playThenDestroy();
        }
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
        if (_AliveTimer < 0)
        {
            Destroy(gameObject);
        }
    }

    void playThenDestroy()
    {
        _ParticleSystem.Play();
        float lifetime = _ParticleSystem.main.duration + _ParticleSystem.main.startLifetime.constantMax;
        Destroy(gameObject, lifetime);
    }

    public void playStart()
    {
        _ParticleSystem.Play();
    }
}
