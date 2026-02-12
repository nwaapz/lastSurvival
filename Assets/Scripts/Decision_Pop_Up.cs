using System;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class Decision_Pop_Up : MonoBehaviour, IDecisionPop
{
    [SerializeField] Transform BtnContent;
    [SerializeField] TextMeshProUGUI headerText, BodyText;
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

        // Disable layout control so tweens can move this popup freely
        var layoutElement = GetComponent<UnityEngine.UI.LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

        // Immediately hide and position at start to prevent flash at center
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        // Initialize DOTween if needed
        DOTween.Init();
        
        if (rectTransform != null)
        {
            initialAnchoredPosition = rectTransform.anchoredPosition;
            Debug.Log($"Decision_Pop_Up Start: initialAnchoredPosition = {initialAnchoredPosition}", this);
            
            // Set start position immediately (above center)
            rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(0f, moveOffset);
        }

        // Wait one frame to ensure layout is settled before animating
        StartCoroutine(PlayShowAnimationDelayed());
    }

    System.Collections.IEnumerator PlayShowAnimationDelayed()
    {
        yield return null; // Wait one frame
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
            Debug.LogWarning("Decision_Pop_Up: Missing RectTransform or CanvasGroup, skipping animation.", this);
            return;
        }

        // Position and alpha already set in Start, just start the tweens
        Debug.Log($"Decision_Pop_Up PlayShowAnimation: starting tweens from {rectTransform.anchoredPosition} to {initialAnchoredPosition}, duration = {showDuration}", this);

        var fadeTween = canvasGroup.DOFade(1f, showDuration);
        var moveTween = rectTransform.DOAnchorPos(initialAnchoredPosition, showDuration).SetEase(Ease.OutCubic);

        Debug.Log($"Decision_Pop_Up PlayShowAnimation: fadeTween active = {fadeTween != null}, moveTween active = {moveTween != null}", this);
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

        Debug.Log("Decision_Pop_Up: PlayHideAnimation", this);
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
