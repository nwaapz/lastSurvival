using UnityEngine;

public class FightSceneSfxManager : SingletonMono<FightSceneSfxManager>, IService
{
    [Header("Audio Source")] 
    [SerializeField] private AudioSource sfxSource;

    [Header("Tier 1 SFX")] 
    [SerializeField] private AudioClip tier1PistolClip;

    [Header("Zombie Scream SFX")]
    [SerializeField] private AudioClip[] zombieScreamClips;

    [Header("Barrel SFX")]
    [SerializeField] private AudioClip barrelHitClip;

    [Header("End Game SFX")]
    [SerializeField] private AudioClip zombieDeathClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    [Header("Ambient SFX")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioClip nightAmbientClip;
    [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.3f;

    [Header("SFX Volume")]
    [SerializeField][Range(0f,1f)] private float zombieDeathClipVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float PistolVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float barrelHitVolume = 1f;


    private bool _initialized = false;

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<FightSceneSfxManager>(this);
        }
    }

    private void Start()
    {
        // Fallback initialization if not registered as a service
        if (!_initialized)
        {
            Init();
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<FightSceneSfxManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
        
        // Start night ambient loop
        StartNightAmbient();
    }
    
    private void StartNightAmbient()
    {
        if (nightAmbientClip == null) return;
        
        // Create ambient audio source if not assigned
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
        }
        
        ambientSource.clip = nightAmbientClip;
        ambientSource.loop = true;
        ambientSource.volume = ambientVolume;
        ambientSource.Play();
    }
    
    /// <summary>
    /// Stops the night ambient sound.
    /// </summary>
    public void StopNightAmbient()
    {
        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientSource.Stop();
        }
    }

    /// <summary>
    /// Play generic weapon SFX from config
    /// </summary>
    public void PlayWeaponSfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Play Tier 1 pistol shot SFX once at the SFX source position
    /// </summary>
    public void PlayTier1Pistol()
    {
        if (tier1PistolClip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(tier1PistolClip, PistolVolume);
    }

    public AudioClip GetRandomZombieScream()
    {
        if (zombieScreamClips == null || zombieScreamClips.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, zombieScreamClips.Length);
        return zombieScreamClips[index];
    }

    /// <summary>
    /// Play Barrel Hit SFX
    /// </summary>
    public void PlayBarrelHit()
    {
        if (barrelHitClip == null || sfxSource == null) return;
        
        // PlayOneShot allows overlapping sounds
        sfxSource.PlayOneShot(barrelHitClip, barrelHitVolume);
    }

    /// <summary>
    /// Play Zombie Death SFX
    /// </summary>
    public void PlayZombieDeath()
    {
        if (zombieDeathClip == null || sfxSource == null) return;
        
        sfxSource.PlayOneShot(zombieDeathClip,zombieDeathClipVolume);
    }

    public static void PlayBarrelHitSfx()
    {
        if (Instance != null)
        {
            Instance.PlayBarrelHit();
        }
    }
    
    public static void PlayZombieDeathSfx()
    {
        if (Instance != null)
        {
            Instance.PlayZombieDeath();
        }
    }


    /// <summary>
    /// Static helper so gameplay code can call without keeping a reference
    /// </summary>
    public static void PlayTier1PistolSfx()
    {
        if (Instance != null)
        {
            Instance.PlayTier1Pistol();
        }
    }

    /// <summary>
    /// Play win SFX when the player wins the fight
    /// </summary>
    public void PlayWinSfx()
    {
        if (winClip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(winClip);
    }

    /// <summary>
    /// Play lose SFX when the player loses the fight
    /// </summary>
    public void PlayLoseSfx()
    {
        if (loseClip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(loseClip);
    }

    /// <summary>
    /// Static helper for win SFX
    /// </summary>
    public static void PlayWin()
    {
        if (Instance != null)
        {
            Instance.PlayWinSfx();
        }
    }

    /// <summary>
    /// Static helper for lose SFX
    /// </summary>
    public static void PlayLose()
    {
        if (Instance != null)
        {
            Instance.PlayLoseSfx();
        }
    }
}
