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

    public void init(float aliveTimer = -1)
    {
        _ParticleSystem = GetComponent<ParticleSystem>();
        if (_ParticleSystem == null)
        {
            Debug.LogError($"ParticleSystem�I�u�W�F�N�g��������Ȃ��̂͂��������I {this.gameObject.name}");
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
