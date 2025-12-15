using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Michsky.UI.Shift;

public class CircleSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float dwellTime = 1.5f;

    [SerializeField]private Image thisSlider;
    private Button thisBtn = default;
    private UIElementSound uiElementSound = default;

    private bool onPointer = default;
    private float value = default;

    // Start is called before the first frame update
    void Start()
    {
        this.value = 0.0f;
        this.onPointer = false;

        if(this.thisSlider == null)
            this.thisSlider = this.GetComponent<Image>();
        this.thisSlider.fillAmount = this.value;

        this.thisBtn = this.GetComponentInParent<Button>();
        this.uiElementSound = this.GetComponentInParent<UIElementSound>();
        this.thisBtn.onClick.AddListener(BtnClicked);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.onPointer && this.thisBtn.interactable)
        {
            this.value += Time.unscaledDeltaTime / this.dwellTime;
            this.thisSlider.fillAmount = this.value;

            if (this.value >= 1.0f)
            {
                this.onPointer = false;
                this.thisBtn.onClick.Invoke();
                this.value = 0;
                this.thisSlider.fillAmount = this.value;
                if (this.uiElementSound != null)
                    this.uiElementSound.OnPointerClick(null);
            }
        }
    }

    void BtnClicked()
    {
        this.onPointer = false;
        this.value = 0;
        this.thisSlider.fillAmount = this.value;

    }

    public void CircleEnter()
    {
        this.onPointer = true;
    }

    public void CircleExit()
    {
        this.onPointer = false;
        this.value = 0;
        this.thisSlider.fillAmount = this.value;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
       this.onPointer = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
       this.onPointer = false;
       this.value = 0;
       this.thisSlider.fillAmount = this.value;
    }

    public void OnDisable()
    {
        this.onPointer = false;
        this.value = 0;
        this.thisSlider.fillAmount = this.value;
    }
}
