using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// A popup variant that displays a header image and/or body image.
/// </summary>
public class Decision_Pop_Up_Image : MonoBehaviour, IDecisionPop
{
    [SerializeField] Transform BtnContent;
    [SerializeField] TextMeshProUGUI headerText, BodyText;
    [SerializeField] Image headerImage;
    [SerializeField] Image bodyImage;
    
    Action OKAction, CancelAction;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float showDuration = 0.3f;
    [SerializeField] float hideDuration = 0.3f;
    [SerializeField] float moveOffset = 200f;

    Vector2 initialAnchoredPosition;

    public event Action<IDecisionPop> OnDestroyed;

    void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        var layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        DOTween.Init();
        
        if (rectTransform != null)
        {
            // Ensure anchors are centered so popup appears in middle of screen
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // Target center of parent (screen center)
            initialAnchoredPosition = Vector2.zero;
            rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(0f, moveOffset);
        }

        StartCoroutine(PlayShowAnimationDelayed());
    }

    System.Collections.IEnumerator PlayShowAnimationDelayed()
    {
        yield return null;
        PlayShowAnimation();
    }

    public void SetHeaderText(string val)
    {
        headerText.text = val;
    }

    public void SetBodyText(string val)
    {
        BodyText.text = val;
    }

    /// <summary>
    /// Sets the header image. Pass null to hide.
    /// </summary>
    public void SetHeaderImage(Sprite sprite)
    {
        if (headerImage != null)
        {
            if (sprite != null)
            {
                headerImage.sprite = sprite;
                headerImage.gameObject.SetActive(true);
            }
            else
            {
                headerImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Sets the body image. Pass null to hide.
    /// </summary>
    public void SetBodyImage(Sprite sprite)
    {
        if (bodyImage != null)
        {
            if (sprite != null)
            {
                bodyImage.sprite = sprite;
                bodyImage.gameObject.SetActive(true);
            }
            else
            {
                bodyImage.gameObject.SetActive(false);
            }
        }
    }

    public void SetOKAction(Action val)
    {
        OKAction = val;
    }

    public void SetCancelAction(Action val)
    {
        CancelAction = val;
    }

    public void SetupButtons(ButtonHelper ok, ButtonHelper cancel, Color okColor, Color cancelColor)
    {
        var buttonPrefab = Resources.Load<UI_Btn>("UI_Btn");
        if (buttonPrefab == null)
        {
            Debug.LogError("UI_Btn prefab not found in Resources folder.");
            return;
        }

        if (ok != null)
        {
            var okBtn = Instantiate(buttonPrefab, BtnContent);
            okBtn.Initialize(ok.BtnText, okColor, () =>
            {
                OKAction?.Invoke();
            });
            okBtn.SetPopup(transform);
        }

        if (cancel != null)
        {
            var cancelBtn = Instantiate(buttonPrefab, BtnContent);
            cancelBtn.Initialize(cancel.BtnText, cancelColor, () =>
            {
                CancelAction?.Invoke();
            });
            cancelBtn.SetPopup(transform);
        }
    }

    public void Close()
    {
        PlayHideAnimation();
    }

    void PlayShowAnimation()
    {
        if (rectTransform == null || canvasGroup == null)
        {
            Debug.LogWarning("Decision_Pop_Up_Image: Missing RectTransform or CanvasGroup, skipping animation.", this);
            return;
        }

        canvasGroup.DOFade(1f, showDuration);
        rectTransform.DOAnchorPos(initialAnchoredPosition, showDuration).SetEase(Ease.OutCubic);
    }

    void PlayHideAnimation()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        Vector2 targetPosition = initialAnchoredPosition - new Vector2(0f, moveOffset);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(canvasGroup.DOFade(0f, hideDuration));
        sequence.Join(rectTransform.DOAnchorPos(targetPosition, hideDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(() =>
        {
            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
        });
    }
}
