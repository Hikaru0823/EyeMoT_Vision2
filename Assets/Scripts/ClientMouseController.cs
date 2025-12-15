using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientMouseController : MonoBehaviour
{
    // Start is called before the first frame update
    public void SetPosition(Vector2 position)
    {
        transform.localPosition = position;
    }
}
