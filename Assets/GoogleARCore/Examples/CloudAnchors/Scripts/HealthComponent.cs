﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618
public class HealthComponent : NetworkBehaviour
{
    public int MaxHealth = 6;

    [SyncVar]
    private int _currentHealth = 3;

    public int GetCurrentHealth() { return _currentHealth; }
    public void IncrementHealth() { _currentHealth++; }
}

#pragma warning restore 618