using System.Collections;
using TMPro;
using UnityEngine;

public class Narration_pop : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI message;
    [SerializeField] SpriteRenderer Character;
    [SerializeField] private SpriteRenderer referenceCharacter;
    
    [Header("Typing Animation Settings")]
    [SerializeField] private float typingSpeed = 0.03f; // Time between each character
    
    [Header("Screen Positioning")]
    [SerializeField] private bool anchorToScreen = true;
    [SerializeField] private Vector2 screenOffset = new Vector2(0.01f, 0.01f); // Offset from bottom-left (in viewport %)
    
    private Coroutine currentTypingCoroutine;
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = Camera.main;
    }
    
    private void OnEnable()
    {
        PositionAtBottomLeft();
    }
    
    private void LateUpdate()
    {
        if (anchorToScreen)
        {
            PositionAtBottomLeft();
        }
    }
    
    /// <summary>
    /// Positions the popup at the bottom-left corner of the screen
    /// </summary>
    private void PositionAtBottomLeft()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }
        
        // Calculate the bottom-left position in world space
        // Using viewport coordinates: (0,0) is bottom-left, (1,1) is top-right
        Vector3 viewportPos = new Vector3(screenOffset.x, screenOffset.y, mainCamera.nearClipPlane + 10f);
        Vector3 worldPos = mainCamera.ViewportToWorldPoint(viewportPos);
        
        // Keep the current Z position to maintain layer ordering
        worldPos.z = transform.position.z;
        
        transform.position = worldPos;
    }

    public void SetCharacterPanel(string message, Sprite pose)
    {
        // Stop any previous typing animation
        if (currentTypingCoroutine != null)
        {
            StopCoroutine(currentTypingCoroutine);
        }
        
        // Set the character pose
        if (Character != null && pose != null)
        {
            Debug.Log($"[Narration_pop] Applying sprite '{pose.name}' to Character SpriteRenderer '{Character.name}'");
            Character.sprite = pose;
            UpdateCharacterScale();
        }
        else
        {
            string poseName = pose != null ? pose.name : "<NULL_SPRITE>";
            string characterName = Character != null ? Character.name : "<NULL_CHARACTER_SPRITE_RENDERER>";
            Debug.LogWarning($"[Narration_pop] Cannot apply pose. Character='{characterName}', pose='{poseName}'");
        }
        
        // Start the typing animation
        currentTypingCoroutine = StartCoroutine(TypeText(message));
    }

    private void UpdateCharacterScale()
    {
        if (Character == null || Character.sprite == null)
        {
            return;
        }

        if (referenceCharacter == null || referenceCharacter.sprite == null)
        {
            return;
        }

        Vector2 targetSize = referenceCharacter.bounds.size;
        Vector2 spriteSize = Character.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f || targetSize.x <= 0f || targetSize.y <= 0f)
        {
            return;
        }

        float scaleX = targetSize.x / spriteSize.x;
        float scaleY = targetSize.y / spriteSize.y;
        float uniformScale = Mathf.Min(scaleX, scaleY);

        Character.transform.localScale = new Vector3(uniformScale, uniformScale, Character.transform.localScale.z);
        Character.transform.position = referenceCharacter.transform.position;
    }
    
    private IEnumerator TypeText(string text)
    {
        // Reset the text
        message.text = "";
        
        // Add characters one by one
        foreach (char c in text)
        {
            message.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Clear the reference when done
        currentTypingCoroutine = null;
    }
    
    /// <summary>
    /// Skip the typing animation and show full text immediately
    /// </summary>
    public void SkipTyping(string fullText)
    {
        if (currentTypingCoroutine != null)
        {
            StopCoroutine(currentTypingCoroutine);
            currentTypingCoroutine = null;
        }
        message.text = fullText;
    }
    
}
