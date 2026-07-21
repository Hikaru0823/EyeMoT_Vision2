using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientMouseController : MonoBehaviour
{
    void Start()
    {
        var parent = transform.parent.GetComponent<RectTransform>();
        var maxLength = Mathf.Max(parent.rect.width, parent.rect.height);
        transform.GetComponent<RectTransform>().sizeDelta = new Vector2(maxLength / 20, maxLength / 20);
    }
    public void SetPosition(Vector2 position)
    {
        transform.localPosition = position;
    }
}
