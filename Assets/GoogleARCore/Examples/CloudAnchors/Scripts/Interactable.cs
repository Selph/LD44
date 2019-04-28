using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618
public class Interactable : NetworkBehaviour
{
    public float Radius = 3.0f;
    
    [SyncVar]
    private NetworkInstanceId _ownerNetId;

    public NetworkInstanceId GetOwnerNetId()
    {
        return _ownerNetId;
    }

    public void SetOwnerNetId(NetworkInstanceId ownerNetId)
    {
        _ownerNetId = ownerNetId;
    }

    void FixedUpdate()
    {
        gameObject.transform.Rotate(0.0f, Time.deltaTime * 100.0f, 0.0f, Space.Self);
    }
}
#pragma warning restore 618
