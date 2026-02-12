using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hides the topmost parent of this button when clicked.
/// Attach this to your Start Button.
/// </summary>
public class HideTopParentOnClick : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, finds the topmost parent. If false, hides the direct parent.")]
    [SerializeField] private bool hideTopmostParent = true;
    
    [Tooltip("Optional: Specify a specific parent to hide (ignores hideTopmostParent if set)")]
    [SerializeField] private GameObject specificParentToHide;
    
    private Button _button;
    
    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(HideParent);
        }
    }
    
    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HideParent);
        }
    }
    
    public void HideParent()
    {
        GameObject targetToHide = null;
        
        if (specificParentToHide != null)
        {
            targetToHide = specificParentToHide;
        }
        else if (hideTopmostParent)
        {
            // Find the topmost parent (but not the Canvas)
            Transform current = transform.parent;
            Transform topParent = current;
            
            while (current != null)
            {
                // Stop at Canvas (we don't want to hide the entire Canvas)
                if (current.GetComponent<Canvas>() != null)
                {
                    break;
                }
                topParent = current;
                current = current.parent;
            }
            
            if (topParent != null)
            {
                targetToHide = topParent.gameObject;
            }
        }
        else
        {
            // Just hide direct parent
            if (transform.parent != null)
            {
                targetToHide = transform.parent.gameObject;
            }
        }
        
        if (targetToHide != null)
        {
            targetToHide.SetActive(false);
            Debug.Log($"[HideTopParentOnClick] Hidden: {targetToHide.name}");
        }
    }
}
