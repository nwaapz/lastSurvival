using System;
using UnityEngine;

public interface IDecisionPop
{
    event Action<IDecisionPop> OnDestroyed;
}
