using GoogleARCore.Examples.CloudAnchors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618
public class HealthComponent : NetworkBehaviour
{
    public int MaxHealth = 6;

    public delegate void HealthChanged(int health);
    public event HealthChanged OnHealthChanged;

    [SyncVar(hook = "UpdateHealth")]
    private int _currentHealth = 6;
    
    public int GetCurrentHealth() { return _currentHealth; }
    public void IncrementHealth() { UpdateHealth(_currentHealth + 1); }
    public void DecrementHealth() { UpdateHealth(_currentHealth - 1); }

    private void Start()
    {
        UpdateHealth(MaxHealth);
    }

    private void UpdateHealth(int newHealth)
    {
        int prev = _currentHealth;
        _currentHealth = Mathf.Min(MaxHealth, Mathf.Max(newHealth, 0));

        if (prev != _currentHealth)
        {
            OnHealthChanged(_currentHealth);
        }
    }
}

#pragma warning restore 618