using System;
using UnityEngine;

public abstract class CombatTarget2D : MonoBehaviour
{
    public static event Action<float, Vector3> Damaged;

    public abstract bool IsAlive { get; }
    public abstract void TakeDamage(float amount);

    protected void ReportDamage(float amount, Vector3 worldPosition)
    {
        if (amount > 0f)
            Damaged?.Invoke(amount, worldPosition);
    }
}
