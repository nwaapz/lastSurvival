using UnityEngine;

public class Costmanager : SingletonMono<Costmanager>, IService
{
    [Header("Costs")]
    [SerializeField] private int newSoldierCost = 100;
    [SerializeField] private int rangeUpgradeCost = 200;
    [SerializeField] private int damageUpgradeCost = 150;
    [SerializeField] private int fireRateUpgradeCost = 150;
    [SerializeField] private int barrelCost = 50;
    [SerializeField] private int gateRepairCost = 100;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);
            
            // Self-register with ServiceLocator
            if (ServiceLocator.HasInstance)
            {
                ServiceLocator.Instance.Register<Costmanager>(this);
            }
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<Costmanager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        // No special initialization needed for now
    }

    public int GetNewSoldierCost()
    {
        return newSoldierCost;
    }

    public int GetRangeUpgradeCost()
    {
        return rangeUpgradeCost;
    }

    public int GetDamageUpgradeCost()
    {
        return damageUpgradeCost;
    }

    public int GetFireRateUpgradeCost()
    {
        return fireRateUpgradeCost;
    }

    public int GetBarrelCost()
    {
        return barrelCost;
    }

    public int GetGateRepairCost()
    {
        return gateRepairCost;
    }
}
