using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Interactable : NetworkBehaviour
{
    public float Radius = 10.0f;

    [SyncVar]
    int playerId = 0;

    void FixedUpdate()
    {
        gameObject.transform.Rotate(0.0f, Time.deltaTime * 360.0f, 0.0f, Space.Self);
    }
}
