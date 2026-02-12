using UnityEngine;

/// <summary>
/// Attribute to mark a string field as a scene name selector.
/// When applied, the Inspector will show a dropdown of all scenes in Build Settings.
/// </summary>
public class SceneNameAttribute : PropertyAttribute
{
    public bool AllowEmpty { get; private set; }
    
    /// <summary>
    /// Creates a scene name selector attribute.
    /// </summary>
    /// <param name="allowEmpty">If true, includes an empty option in the dropdown.</param>
    public SceneNameAttribute(bool allowEmpty = true)
    {
        AllowEmpty = allowEmpty;
    }
}
