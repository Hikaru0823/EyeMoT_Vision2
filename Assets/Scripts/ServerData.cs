using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Server/Data")]
public class ServerData : ScriptableObject
{
    public int[] PortPresets = new int[] { 8080, 8081, 54000, 55000 };
    public int DictionaryPort_UDP = 53000;
}
