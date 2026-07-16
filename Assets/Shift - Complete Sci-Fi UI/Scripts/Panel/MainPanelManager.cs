using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Michsky.UI.Shift
{
    public class MainPanelManager : MonoBehaviour
    {
        [Header("Panel List")]
        public List<PanelItem> panels = new List<PanelItem>();

        [Header("Settings")]
        public int currentPanelIndex = 0;
        private int newPanelIndex;
        public int currentButtonIndex = 0;

        private GameObject currentPanel;
        private GameObject nextPanel;

        private Animator currentPanelAnimator;
        private Animator nextPanelAnimator;

        private Animator currentButtonAnimator;
        private Animator nextButtonAnimator;

        string panelFadeIn = "Panel In";
        string panelFadeOut = "Panel Out";
        string buttonFadeIn = "Normal to Pressed";
        string buttonFadeOut = "Pressed to Dissolve";

        [System.Serializable]
        public class PanelItem
        {
            public string panelName;
            public GameObject panelObject;
            public Object buttonObject;
        }

        void OnEnable()
        {
            if (!IsValidPanelIndex(currentPanelIndex))
                return;

            currentButtonIndex = currentPanelIndex;

            currentPanel = panels[currentPanelIndex]?.panelObject;
            currentPanelAnimator = currentPanel?.GetComponent<Animator>();
            currentPanelAnimator?.Play(panelFadeIn);

            currentButtonAnimator = GetButtonAnimator(currentPanelIndex);
            currentButtonAnimator?.Play(buttonFadeIn);

            StartCoroutine("DisablePreviousPanel");
        }

        public void OpenPanel(string newPanel)
        {
            bool panelFound = false;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i]?.panelName == newPanel)
                {
                    newPanelIndex = i;
                    panelFound = true;
                    break;
                }
            }

            if (panelFound && newPanelIndex != currentPanelIndex && IsValidPanelIndex(currentPanelIndex) && IsValidPanelIndex(newPanelIndex))
            {
                StopCoroutine("DisablePreviousPanel");

                //パネルの管理
                //移動前パネルと移動後パネルを取得
                currentPanel = panels[currentPanelIndex]?.panelObject;
                currentPanelIndex = newPanelIndex;
                nextPanel = panels[currentPanelIndex]?.panelObject;
                nextPanel?.SetActive(true);

                //パネルのアニメーション管理
                currentPanelAnimator = currentPanel?.GetComponent<Animator>();
                nextPanelAnimator = nextPanel?.GetComponent<Animator>();
                currentPanelAnimator?.Play(panelFadeOut);
                nextPanelAnimator?.Play(panelFadeIn);

                //ボタンの管理
                //移動前ボタンと移動後ボタンを取得
                currentButtonAnimator = GetButtonAnimator(currentButtonIndex);
                currentButtonIndex = newPanelIndex;
                nextButtonAnimator = GetButtonAnimator(currentButtonIndex);

                //ボタンのアニメーション管理
                currentButtonAnimator?.Play(buttonFadeOut);
                nextButtonAnimator?.Play(buttonFadeIn);

                if(gameObject.activeInHierarchy)
                    StartCoroutine("DisablePreviousPanel");
            }
        }

        public string GetCurrentPanelName()
        {
            if (!IsValidPanelIndex(currentPanelIndex))
                return string.Empty;

            return panels[currentPanelIndex]?.panelName;
        }

        IEnumerator DisablePreviousPanel()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            for (int i = 0; i < panels.Count; i++)
            {
                if (i == currentPanelIndex)
                    continue;

                panels[i]?.panelObject?.SetActive(false);
            }
        }

        private bool IsValidPanelIndex(int index)
        {
            return panels != null && index >= 0 && index < panels.Count;
        }

        private Animator GetButtonAnimator(int index)
        {
            if (!IsValidPanelIndex(index))
                return null;

            Object buttonObject = panels[index]?.buttonObject;

            if (buttonObject is GameObject buttonGameObject)
                return buttonGameObject.GetComponent<Animator>();

            if (buttonObject is Component buttonComponent)
                return buttonComponent.GetComponent<Animator>();

            return null;
        }
    }
}
