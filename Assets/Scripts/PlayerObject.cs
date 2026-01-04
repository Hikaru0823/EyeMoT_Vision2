using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    public static PlayerObject Local = null;
    public ClientMouseController MouseController;
    public GameObject ViewPanel;
    public int Id;
}
