using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Michsky.UI.Shift
{
    public class SwitchManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Each switch must have a different tag")]
        [SerializeField] private string switchTag = "Switch";
        public bool isOn = true;
        public bool saveValue = true;
        public bool invokeAtStart = true;

        [Header("Events")]
        public UnityEvent<bool> ToggleEvents;

        [HideInInspector] public Animator switchAnimator;
        Button switchButton;

        void Start()
        {
            if (invokeAtStart == true) { ToggleEvents.Invoke(isOn); }
        }

        void OnEnable()
        {
            if (switchAnimator == null) { switchAnimator = gameObject.GetComponent<Animator>(); }
            if (switchButton == null) { switchButton = gameObject.GetComponent<Button>(); switchButton.onClick.AddListener(AnimateSwitch); }
            bool state = ES3.Load<bool>(SaveKey.SWITCH_STATE + switchTag, defaultValue:isOn);

            if (saveValue == true)
            {
                if (state)
                {
                    switchAnimator.Play("Switch On");
                    isOn = true;
                }

                else
                {
                    switchAnimator.Play("Switch Off");
                    isOn = false;
                }
            }

            else
            {
                if (isOn == true) { switchAnimator.Play("Switch On"); isOn = true; }
                else { switchAnimator.Play("Switch Off"); isOn = false; }
            }


        }

        public void AnimateSwitch()
        {
            if (isOn == true)
            {
                switchAnimator.Play("Switch Off");
                isOn = false;
                ToggleEvents.Invoke(isOn);
                if (saveValue == true) { ES3.Save<bool>(SaveKey.SWITCH_STATE + switchTag, false); }
            }

            else
            {
                switchAnimator.Play("Switch On");
                isOn = true;
                ToggleEvents.Invoke(isOn);
                if (saveValue == true) { ES3.Save<bool>(SaveKey.SWITCH_STATE + switchTag, true); }
            }
        }
    }
}