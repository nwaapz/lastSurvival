using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseBuilderUIManager : SingletonMono<BaseBuilderUIManager>, IService
{
    public Color OkBtnColor, CancelBtnColor;
    [SerializeField] Transform PopUpParent;
    [SerializeField] Narration_manager naration_manager;
    
    IDecisionPop _currentPopup;
    Queue<PopupRequest> _popupQueue = new Queue<PopupRequest>();
    Queue<ImagePopupRequest> _imagePopupQueue = new Queue<ImagePopupRequest>();
    [SerializeField] Sprite blacksmithUpgradeImage,blackSmithHeaderImage;

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<BaseBuilderUIManager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<BaseBuilderUIManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {

    }


    public void ShowDecisionPopUp(string header, string body, ButtonHelper ok, ButtonHelper cancel)
    {
        var request = new PopupRequest
        {
            Header = header,
            Body = body,
            Ok = ok,
            Cancel = cancel
        };

        // If there's already an active popup, queue this request
        if (_currentPopup != null)
        {
            _popupQueue.Enqueue(request);
            return;
        }

        ShowPopupInternal(request);
    }

    /// <summary>
    /// Shows a popup with header image and/or body image. Pass null for either to hide that image.
    /// </summary>
    public void ShowImagePopUp(string header, string body, Sprite headerImage, Sprite bodyImage, ButtonHelper ok, ButtonHelper cancel)
    {
        var request = new ImagePopupRequest
        {
            Header = header,
            Body = body,
            HeaderImage = headerImage,
            BodyImage = bodyImage,
            Ok = ok,
            Cancel = cancel
        };

        if (_currentPopup != null)
        {
            _imagePopupQueue.Enqueue(request);
            return;
        }

        ShowImagePopupInternal(request);
    }

    void ShowImagePopupInternal(ImagePopupRequest request)
    {
        var popModel = Resources.Load<Decision_Pop_Up_Image>("Decision_Pop_Up_Image");
        if (popModel == null)
        {
            Debug.LogError("Decision_Pop_Up_Image prefab not found in Resources. Expected path: Resources/Decision_Pop_Up_Image.prefab");
            return;
        }

        var popUp = Instantiate(popModel, PopUpParent);

        popUp.SetHeaderText(request.Header);
        popUp.SetBodyText(request.Body);
        popUp.SetHeaderImage(request.HeaderImage);
        popUp.SetBodyImage(request.BodyImage);

        if (request.Ok != null)
        {
            popUp.SetOKAction(request.Ok.OnPress);
        }

        if (request.Cancel != null)
        {
            popUp.SetCancelAction(request.Cancel.OnPress);
        }

        popUp.SetupButtons(request.Ok, request.Cancel, OkBtnColor, CancelBtnColor);

        _currentPopup = popUp;
        _currentPopup.OnDestroyed += HandlePopupDestroyed;
    }

    void ShowPopupInternal(PopupRequest request)
    {
        var popModel = Resources.Load<Decision_Pop_Up>("Decision_Pop_Up");
        if (popModel == null)
        {
            Debug.LogError("Decision_Pop_Up prefab not found in Resources. Expected path: Resources/Decision_Pop_Up.prefab or matching Resources path.");
            return;
        }

        var popUp = Instantiate(popModel, PopUpParent);

        popUp.SetHeaderText(request.Header);
        popUp.SetBodyText(request.Body);

        if (request.Ok != null)
        {
            popUp.SetOKAction(request.Ok.OnPress);
        }

        if (request.Cancel != null)
        {
            popUp.SetCancelAction(request.Cancel.OnPress);
        }

        popUp.SetupButtons(request.Ok, request.Cancel, OkBtnColor, CancelBtnColor);

        // Track this popup and listen for when it's destroyed
        _currentPopup = popUp;
        _currentPopup.OnDestroyed += HandlePopupDestroyed;
    }

    void HandlePopupDestroyed(IDecisionPop pop)
    {
        if (_currentPopup == pop)
        {
            _currentPopup.OnDestroyed -= HandlePopupDestroyed;
            _currentPopup = null;

            // Show next popup in queue if any
            if (_popupQueue.Count > 0)
            {
                var next = _popupQueue.Dequeue();
                ShowPopupInternal(next);
            }
            else if (_imagePopupQueue.Count > 0)
            {
                var next = _imagePopupQueue.Dequeue();
                ShowImagePopupInternal(next);
            }
        }
    }

    class PopupRequest
    {
        public string Header;
        public string Body;
        public ButtonHelper Ok;
        public ButtonHelper Cancel;
    }

    class ImagePopupRequest
    {
        public string Header;
        public string Body;
        public Sprite HeaderImage;
        public Sprite BodyImage;
        public ButtonHelper Ok;
        public ButtonHelper Cancel;
    }

    [ContextMenu("Test Decision Popup - OK Only")]
    private void TestDecisionPopupOkOnly()
    {
        var ok = new ButtonHelper
        {
            BtnText = "OK",
            OnPress = () => Debug.Log("OK pressed from TestDecisionPopupOkOnly")
        };

        ShowDecisionPopUp("Test OK Only", "This is a test popup with only OK button.", ok, null);
    }

    [ContextMenu("Test Decision Popup - OK and Cancel")]
    private void TestDecisionPopupOkAndCancel()
    {
        var ok = new ButtonHelper
        {
            BtnText = "Confirm",
            OnPress = () => Debug.Log("Confirm pressed from TestDecisionPopupOkAndCancel")
        };

        var cancel = new ButtonHelper
        {
            BtnText = "Cancel",
            OnPress = () => Debug.Log("Cancel pressed from TestDecisionPopupOkAndCancel")
        };

        ShowDecisionPopUp("Test OK & Cancel", "This is a test popup with both buttons.", ok, cancel);
    }

    [ContextMenu("Test Decision Popup - Custom Text")]
    private void TestDecisionPopupCustomText()
    {
        var ok = new ButtonHelper
        {
            BtnText = "Yes, Do It",
            OnPress = () => Debug.Log("Custom YES pressed")
        };

        var cancel = new ButtonHelper
        {
            BtnText = "No, Thanks",
            OnPress = () => Debug.Log("Custom NO pressed")
        };

        ShowDecisionPopUp("Custom Buttons", "Check button texts and colors.", ok, cancel);
    }

    [ContextMenu("Test Image Popup - Header Image Only")]
    private void TestImagePopupHeaderOnly()
    {
        var ok = new ButtonHelper
        {
            BtnText = "Got It",
            OnPress = () => Debug.Log("OK pressed from TestImagePopupHeaderOnly")
        };

        ShowImagePopUp("Header Image Test", "This popup shows a header image.", blackSmithHeaderImage, null, ok, null);
    }

    [ContextMenu("Test Image Popup - Body Image Only")]
    private void TestImagePopupBodyOnly()
    {
        var ok = new ButtonHelper
        {
            BtnText = "Got It",
            OnPress = () => Debug.Log("OK pressed from TestImagePopupBodyOnly")
        };

        ShowImagePopUp("Body Image Test", "This popup shows a body image.", null, blacksmithUpgradeImage, ok, null);
    }

    [ContextMenu("Test Image Popup - Both Images")]
    private void TestImagePopupBoth()
    {
        var ok = new ButtonHelper
        {
            BtnText = "Confirm",
            OnPress = () => Debug.Log("Confirm pressed from TestImagePopupBoth")
        };

        var cancel = new ButtonHelper
        {
            BtnText = "Cancel",
            OnPress = () => Debug.Log("Cancel pressed from TestImagePopupBoth")
        };

        ShowImagePopUp("Both Images Test", "This popup shows header and body images.", blackSmithHeaderImage, blacksmithUpgradeImage, ok, cancel);
    }

}

public class ButtonHelper
{
    public string BtnText;
    public Action OnPress;
}