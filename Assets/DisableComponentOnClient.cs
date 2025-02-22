using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DisableComponentOnClient : NetworkBehaviour
{
    [SerializeField] private Behaviour _component;
    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
            _component.enabled = false;
    }
}
