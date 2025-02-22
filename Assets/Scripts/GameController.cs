using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : NetworkBehaviour
{
    [SerializeField] private float _scoreToWin;
    [SerializeField] private List<Image> _playerScoreBars = new List<Image>();
    [SerializeField] private PolygonCollider2D _arenaCollider;
    [SerializeField] private GameObject _spotLight;
    [SerializeField] private GameObject _matchScreen;
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private TMP_Text _victoryText;
    [SerializeField] private PlayerMovement _playerPrefab;
    [SerializeField] private GameObject _startGameButton;
    [SerializeField] private List<GameObject> _readyImages;

    private NetworkList<int> _playerScores;
    private NetworkList<bool> _playerReady;

    private const int MaxPlayers = 4; // Set the max player limit
    private HashSet<ulong> _approvedPlayers = new HashSet<ulong>(); // Track approved players
    private bool _gameStarted;
    private bool _gameEnded;

    private void Awake()
    {
        _playerScores = new NetworkList<int>(new List<int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        _playerReady = new NetworkList<bool>(new List<bool>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        // Manually initialize the list to size 4 with default values (0)
        if (IsHost)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                _playerScores.Add(0);
                _playerReady.Add(false);
            }
        }
        else
        {
            _startGameButton.gameObject.SetActive(false);
        }
        _playerScores.OnListChanged += UpdateScore;
        _playerReady.OnListChanged += UpdateReadyImages;
    }

    private void UpdateReadyImages(NetworkListEvent<bool> changeEvent)
    {
        for (var i = 0; i < _playerReady.Count; i++)
        {
            _readyImages[i].SetActive(_playerReady[i]);
        }
    }

    public void CheckPlayerReady()
    {
        if (!IsHost) return;
        var playerCount = _approvedPlayers.Count;
        if (playerCount < 2) return;
        var everyOneIsReady = true;
        for (var i = 0; i < playerCount; i++)
        {
            bool player = _playerReady[i];
            if (!player)
            {
                everyOneIsReady = false;
            }
        }
        if (everyOneIsReady)
            StartGame();
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
        EventManager.AddListener<ulong, int>("ChangePlayerScore", ChangePlayerScore);
        EventManager.AddListener<Transform, NetworkVariable<bool>>("PlayerRespawn", RespawnPlayer);
    }

    public override void OnDestroy()
    {
        EventManager.RemoveListener<ulong, int>("ChangePlayerScore", ChangePlayerScore);
        EventManager.RemoveListener<Transform, NetworkVariable<bool>>("PlayerRespawn", RespawnPlayer);
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
        if (_playerScores != null)
        {
            _playerScores.Dispose();
            _playerScores = null; // Prevent potential reuse of disposed object
        }
        if (_playerReady != null)
        {
            _playerReady.Dispose();
            _playerReady = null; // Prevent potential reuse of disposed object
        }
    }

    public void TogglePlayerReady()
    {
        TogglePlayerReadyServerRpc(NetworkManager.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TogglePlayerReadyServerRpc(ulong clientId)
    {
        if (_gameStarted) return;
        if (_approvedPlayers.Contains(clientId))
        {
            var playerIndex = GetPlayerIndex(clientId);
            _playerReady[playerIndex] = !_playerReady[playerIndex];

            Debug.Log("Ready do " + playerIndex + " ficou " + _playerReady[playerIndex]);
        }
    }

    private void StartGame()
    {
        StartGameClientRpc();
        foreach (var player in _approvedPlayers)
        {
            var playerObject = Instantiate(_playerPrefab);
            playerObject.playerId.Value = GetPlayerIndex(player);
            playerObject.NetworkObject.SpawnAsPlayerObject(player, true);
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        _gameStarted = true;
        _matchScreen.SetActive(false);
    }

    private void RespawnPlayer(Transform player, NetworkVariable<bool> fell)
    {
        StartCoroutine(RespawnPlayerCoroutine(player, fell));
    }

    private IEnumerator RespawnPlayerCoroutine(Transform player, NetworkVariable<bool> fell)
    {
        player.position = GetRandomPointInsideArena();
        yield return new WaitForSeconds(2.5f);
        if (IsSpawned)
            fell.Value = false;
    }

    private void UpdateScore(NetworkListEvent<int> changeEvent)
    {
        _playerScoreBars[changeEvent.Index].fillAmount = changeEvent.Value / _scoreToWin;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;

        if (currentPlayers < MaxPlayers)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            _approvedPlayers.Add(request.ClientNetworkId); // Mark the player as approved
        }
        else
        {
            response.Approved = false; // Reject connection
        }

        response.Pending = false; // Approval process complete
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (_gameEnded) return;
        Debug.Log($"Client {clientId} disconnected.");

        if (_approvedPlayers.Contains(clientId) && _gameStarted)
        {
            Debug.Log("Match player disconnected. Shutting down room.");
            ShutdownRoom();
        }
        if (_approvedPlayers.Contains(clientId))
        {
            _playerReady[GetPlayerIndex(clientId)] = false;
        }
        else if (clientId == NetworkManager.Singleton.LocalClientId && !IsServer)
        {
            Debug.Log("Disconnected from server...");
            HandleRejectionUI();
        }
    }

    private void HandleRejectionUI()
    {
        StartCoroutine(ShutdownAndReload());
        SceneManager.LoadScene("GameScene");
        Debug.Log("Implement UI feedback here.");
    }

    private void ShutdownRoom()
    {
        if (IsServer)
        {
            Debug.Log("Shutting down the server and returning to main menu.");
            StartCoroutine(ShutdownAndReload());
        }
    }

    private IEnumerator ShutdownAndReload()
    {
        NetworkManager.Singleton.Shutdown();

        // Wait until NetworkManager is fully shut down
        while (NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        // Now it's safe to reload the scene
        SceneManager.LoadScene("GameScene");
    }


    // Method to increase player score
    private void ChangePlayerScore(ulong playerId, int amount)
    {
        int index = GetPlayerIndex(playerId);
        if (index >= 0 && index < _playerScores.Count)
        {
            if (_playerScores[index] + amount > 0)
                _playerScores[index] += amount;
            else
                _playerScores[index] = 0;
            CheckForWin(index);
        }
    }

    // Check if a player has won
    private void CheckForWin(int playerIndex)
    {
        if (_playerScores[playerIndex] >= _scoreToWin)
        {
            FinishGameClientRpc(playerIndex);
        }
    }

    [ClientRpc]
    private void FinishGameClientRpc(int playerWon)
    {
        _gameEnded = true;
        _spotLight.SetActive(false);
        _victoryPanel.SetActive(true);
        _victoryText.text = $"and the Oscar goes to...\nPlayer {playerWon + 1}!";
        NetworkManager.Singleton.Shutdown();
    }

    // Find player index based on ID
    private int GetPlayerIndex(ulong playerId)
    {
        int index = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == playerId)
            {
                return index;
            }
            index++;
        }
        return -1; // Player not found
    }

    public Vector2 GetRandomPointInsideArena()
    {
        if (_arenaCollider == null)
        {
            Debug.LogError("PolygonCollider2D is not assigned.");
            return Vector2.zero;
        }

        Bounds bounds = _arenaCollider.bounds;
        Vector2 randomPoint;

        // Try finding a valid point inside the polygon
        int maxAttempts = 10; // Prevent infinite loops
        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            randomPoint = new Vector2(randomX, randomY);

            // Check if the point is inside the polygon
            if (_arenaCollider.OverlapPoint(randomPoint))
            {
                return randomPoint;
            }
        }

        Debug.LogWarning("Failed to find a valid point inside the polygon.");
        return _arenaCollider.transform.position; // Fallback to polygon center
    }
}
