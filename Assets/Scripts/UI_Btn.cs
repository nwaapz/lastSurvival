using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Btn : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI btnText;
    [SerializeField] Button button;
    [SerializeField] Transform Popup;
    [SerializeField] Image image;

    public void SetBtn(string text,Transform parent)
    {
        btnText.text = text;
        Popup = parent;
    }   

    public void Initialize(string text, Color color, Action onClick)
    {
        btnText.text = text;
        image.color = color;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onClick?.Invoke();
                if (Popup != null)
                {
                    var decisionPop = Popup.GetComponent<Decision_Pop_Up>();
                    if (decisionPop != null)
                    {
                        decisionPop.Close();
                    }
                    else
                    {
                        Destroy(Popup.gameObject);
                    }
                }
            });
        }
    }

    public void SetPopup(Transform popup)
    {
        Popup = popup;
    }

}
