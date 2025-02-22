using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System;

public class NetworkManagerUIController : NetworkBehaviour
{
    [SerializeField] private GameObject _matchScreen;
    [SerializeField] private GameObject _customizeScreen;
    [SerializeField] private GameObject _roomScreen;

    [SerializeField] private Button _hostbutton;
    [SerializeField] private Button _clientbutton;
    [SerializeField] private Button _backbutton;
    [SerializeField] private TMP_InputField _roomCodeInputField;
    [SerializeField] private TMP_Text _matchCode;

    private void Start()
    {
        EventManager.AddListener<string>("UpdateMatchCode", UpdateMatchCode);
        EventManager.AddListener("ResetNetworkUI", ResetInteractables);
        _hostbutton.onClick.AddListener(StartHost);
        _clientbutton.onClick.AddListener(StartClient);
    }

    public override void OnDestroy()
    {
        EventManager.RemoveListener<string>("UpdateMatchCode", UpdateMatchCode);
        EventManager.RemoveListener("ResetNetworkUI", ResetInteractables);
        _hostbutton.onClick.RemoveListener(StartHost);
        _clientbutton.onClick.RemoveListener(StartClient);
    }
    private void ResetInteractables()
    {
        _hostbutton.interactable = true;
        _clientbutton.interactable = true;
        _backbutton.interactable = true;
        _roomCodeInputField.interactable = true;
    }

    public override void OnNetworkSpawn()
    {
        _matchScreen.SetActive(false);
    }

    private void UpdateMatchCode(string matchCode)
    {
        _matchCode.text = matchCode;
    }

    private void StartHost()
    {
        EventManager.TriggerEvent("CreateRoom");
    }

    private void StartClient()
    {
        EventManager.TriggerEvent("JoinRoom", _roomCodeInputField.text);
    }
}
