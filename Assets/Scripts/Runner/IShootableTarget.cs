using UnityEngine;

/// <summary>
/// Interface for any object that can be targeted and shot by players.
/// Implement this on zombies, barrels, or any other shootable targets.
/// </summary>
public interface IShootableTarget
{
    /// <summary>
    /// Is this target currently active and can be shot?
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// The transform of this target for aiming/distance calculations
    /// </summary>
    Transform TargetTransform { get; }
    
    /// <summary>
    /// Take damage from a projectile
    /// </summary>
    void TakeDamage(float damage);
}
