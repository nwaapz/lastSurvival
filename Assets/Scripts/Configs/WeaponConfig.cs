using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Configs/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Visuals (Finds child on Player)")]
        [Tooltip("Name of the child object on the Player to enable as the weapon model.")]
        public string weaponModelName;
        
        [Header("Projectile")]
        [Tooltip("Projectile prefab to spawn")]
        public GameObject projectilePrefab;
        [Tooltip("Effect to spawn at fire point")]
        public GameObject muzzleFlashPrefab;
        [Tooltip("Name of the child object on the Weapon Model to use as the Muzzle Point. If found, bullets and flashes spawn here.")]
        public string muzzlePointName;

        [Header("Stats")]
        [Tooltip("Shots per second")]
        public float fireRate = 2f;
        [Tooltip("Damage per shot")]
        public float damage = 10f;
        [Tooltip("Maximum travel distance (converted to lifetime based on speed)")]
        public float bulletRange = 30f;
        
        [Header("Multishot")]
        [Tooltip("Number of bullets fired per shot")]
        public int bulletCount = 1;
        [Tooltip("Horizontal spacing between bullets")]
        public float bulletSpacing = 0.5f;
        
        [Header("Audio")]
        public AudioClip shootAudio;
        [Range(0f, 1f)]
        public float audioVolume = 1f;
    }
}
