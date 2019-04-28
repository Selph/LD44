using GoogleARCore.Examples.CloudAnchors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618
public class HealthComponent : NetworkBehaviour
{
    public int MaxHealth = 6;

    [SyncVar]
    private int _currentHealth = 6;
    private CloudAnchorsExampleController m_CloudAnchorsExampleController;

    public int GetCurrentHealth() { return _currentHealth; }
    public void IncrementHealth() { _currentHealth++; m_CloudAnchorsExampleController.heartsBar.current = _currentHealth; }

    public void DecrementHealth() { _currentHealth = Mathf.Max(_currentHealth -1, 0); m_CloudAnchorsExampleController.heartsBar.current = _currentHealth; }

    private void Start()
    {
        m_CloudAnchorsExampleController =
    GameObject.Find("CloudAnchorsExampleController")
        .GetComponent<CloudAnchorsExampleController>();

        _currentHealth = MaxHealth;

        m_CloudAnchorsExampleController.heartsBar.total = MaxHealth;
        m_CloudAnchorsExampleController.heartsBar.current = _currentHealth;
    }
}

#pragma warning restore 618