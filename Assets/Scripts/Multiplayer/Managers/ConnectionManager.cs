using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using TMPro;
using System;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private UnityTransport _unityTransport;

    private void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private async void Start()
    {
        //Autenrticação no Unity Services
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Connected " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        EventManager.AddListener("CreateRoom", CreateRelay);
        EventManager.AddListener<string>("JoinRoom", JoinRelay);
    }

    private void OnDestroy()
    {
        EventManager.RemoveListener("CreateRoom", CreateRelay);
        EventManager.RemoveListener<string>("JoinRoom", JoinRelay);
    }

    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        _unityTransport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key, allocation.ConnectionData);

        EventManager.TriggerEvent("UpdateMatchCode", joinCode);
        NetworkManager.Singleton.StartHost();
        }
        catch (Exception)
        {
            EventManager.TriggerEvent("ResetNetworkUI");
        }

    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            try
            {
                EventManager.TriggerEvent("UpdateMatchCode", joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            _unityTransport.SetClientRelayData(joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData);
                NetworkManager.Singleton.StartClient();
            }
            catch (Exception)
            {
                EventManager.TriggerEvent("ResetNetworkUI");
            }
        }
        catch (RelayServiceException ex)
        {
            EventManager.TriggerEvent("ResetNetworkUI");
            Debug.Log(ex);
        }
    }
}