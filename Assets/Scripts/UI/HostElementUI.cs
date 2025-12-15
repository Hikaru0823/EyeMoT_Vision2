using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostElementUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _IPAdressText;
    [SerializeField] private TextMeshProUGUI   _portText;
    [SerializeField] private TextMeshProUGUI   _playersText;
    [SerializeField] private TextMeshProUGUI   _pingText;
    [SerializeField] private Image           _pingGage;

    public void SetHostInfo(string ipAdress, string port, string players)
    {
        _IPAdressText.text = ipAdress;
        _portText.text = port;
        _playersText.text = players;
    }

    public void UpdatePing(int ping)
    {
        _pingText.text = ping.ToString();
        _pingGage.fillAmount = 1 - Mathf.Clamp01(ping / 300f);
        if(_pingGage.fillAmount > 0.5f)
        {
            _pingGage.color = Color.green;
        }
        else if(_pingGage.fillAmount > 0.2f)
        {
            _pingGage.color = Color.yellow;
        }
        else
        {
            _pingGage.color = Color.red;
        }
    }
}