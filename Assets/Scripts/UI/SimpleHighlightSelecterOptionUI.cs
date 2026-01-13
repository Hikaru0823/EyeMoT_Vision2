using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleHighlightSelecterOptionUI : MonoBehaviour
{
    private OptionButtonResources[] options;

    string buttonFadeIn = "Normal to Pressed";
    string buttonFadeOut = "Pressed to Dissolve";
    string buttonFadeNormal = "Pressed to Normal";

    public void Init(OptionButtonResources[] _options)
    {
        options = _options;
        foreach (var option in options)
        {
            // 各ボタンのonClickリスナーを設定
            option.Button.onClick.AddListener(() => {
                OnOptionSelected(option);
            });
        }
    }

    public virtual void OnOptionSelected(OptionButtonResources option)
    {
        foreach (var opt in options)
        {
            // ハイライトの表示・非表示を切り替え
            if (opt.Animation != null)
            {
                if(opt == option)
                {
                    opt.Animation.Play(buttonFadeIn);
                }
                else
                {
                    if(opt.Animation.GetCurrentAnimatorStateInfo(0).IsName(buttonFadeIn))
                        opt.Animation.Play(buttonFadeOut);
                }
            }
        }
    }

    /// <summary>
    /// インスペクターで値が変更された時に呼ばれる（エディタのみ）
    /// </summary>
    public void AutoAssign(OptionButtonResources[] _options)
    {
        options = _options;
        if (options != null)
        {
            foreach (var option in options)
            {
                if (option != null)
                {
                    // 各ColorOptionのImageコンポーネント自動割り当てを実行
                    option.ValidateComponents();
                }
            }
        }
    }
}

[Serializable]
public class OptionButtonResources
{
    public Button Button;
    public Image PreviewImage;
    public Animator Animation;

    /// <summary>
    /// ゲームオブジェクトから各コンポーネントを自動取得
    /// </summary>
    private void AutoAssign()
    {
        if (Button != null)
        {
            PreviewImage = Button.transform.Find("Icon")?.GetComponent<Image>();
            Animation = Button.GetComponent<Animator>();
        }
        else
        {
            Debug.LogWarning("Button is null, cannot auto-assign components.");
            PreviewImage = null;
            Animation = null;
        }
    }

    /// <summary>
    /// インスペクターでの変更を検証してコンポーネントを自動割り当て（エディタのみ）
    /// </summary>
    public void ValidateComponents()
    {
        AutoAssign();
    }
}
