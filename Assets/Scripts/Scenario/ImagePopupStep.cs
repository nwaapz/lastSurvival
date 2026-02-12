using UnityEngine;

[CreateAssetMenu(fileName = "ImagePopupStep", menuName = "Scenario/Image Popup Step")]
public class ImagePopupStep : ScenarioStep
{
    [Header("Popup Content")]
    public string headerText;
    [TextArea(2, 4)]
    public string bodyText;
    
    [Header("Images")]
    public Sprite headerImage;
    public Sprite bodyImage;
    
    [Header("Buttons")]
    public string okButtonText = "OK";
    public bool showCancelButton = false;
    public string cancelButtonText = "Cancel";
    
    [Header("Timing")]
    [Tooltip("Delay in seconds before showing the popup")]
    public float delayBeforeShow = 0f;

    private bool _popupClosed;
    private float _delayTimer;
    private bool _popupShown;

    public override void OnEnter()
    {
        _popupClosed = false;
        _popupShown = false;
        _delayTimer = 0f;
        
        // If no delay, show immediately
        if (delayBeforeShow <= 0f)
        {
            ShowPopup();
        }
    }
    
    private void ShowPopup()
    {
        if (_popupShown) return;
        _popupShown = true;

        var ok = new ButtonHelper
        {
            BtnText = okButtonText,
            OnPress = () => _popupClosed = true
        };

        ButtonHelper cancel = null;
        if (showCancelButton)
        {
            cancel = new ButtonHelper
            {
                BtnText = cancelButtonText,
                OnPress = () => _popupClosed = true
            };
        }

        if (BaseBuilderUIManager.Instance != null)
        {
            BaseBuilderUIManager.Instance.ShowImagePopUp(headerText, bodyText, headerImage, bodyImage, ok, cancel);
        }
        else
        {
            Debug.LogWarning("[ImagePopupStep] BaseBuilderUIManager not found, skipping popup.");
            _popupClosed = true;
        }
    }

    public override bool UpdateStep()
    {
        // Handle delay before showing popup
        if (!_popupShown && delayBeforeShow > 0f)
        {
            _delayTimer += Time.deltaTime;
            if (_delayTimer >= delayBeforeShow)
            {
                ShowPopup();
            }
            return false;
        }
        
        return _popupClosed;
    }

    public override void OnExit()
    {
        // Popup closes itself when button is pressed
    }
}
