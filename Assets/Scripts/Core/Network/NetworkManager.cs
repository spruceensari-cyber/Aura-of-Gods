using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Core network synchronization manager for 5v5 multiplayer
/// Handles state sync, RPC calls, and player data management
/// </summary>
public class NetworkManager : MonoBehaviour
{
    [SerializeField] private float tickRate = 20f; // 50ms per tick = 20 ticks/sec
    [SerializeField] private float snapshotInterval = 0.1f;
    
    private static NetworkManager instance;
    private Dictionary<int, PlayerNetworkData> connectedPlayers = new();
    private List<GameStateSnapshot> stateHistory = new();
    private float nextTickTime;
    private int currentTick;
    
    public UnityEvent<PlayerNetworkData> OnPlayerJoined { get; } = new();
    public UnityEvent<int> OnPlayerLeft { get; } = new();
    public UnityEvent<GameStateSnapshot> OnStateUpdate { get; } = new();
    
    public static NetworkManager Instance => instance ?? (instance = FindObjectOfType<NetworkManager>());
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    void Start()
    {
        nextTickTime = Time.time + (1f / tickRate);
    }
    
    void Update()
    {
        if (Time.time >= nextTickTime)
        {
            ProcessNetworkTick();
            nextTickTime = Time.time + (1f / tickRate);
        }
    }
    
    private void ProcessNetworkTick()
    {
        currentTick++;
        
        // Broadcast current game state to all players
        var snapshot = CaptureGameState();
        stateHistory.Add(snapshot);
        
        // Keep only last 1 second of history
        if (stateHistory.Count > tickRate)
            stateHistory.RemoveAt(0);
        
        OnStateUpdate?.Invoke(snapshot);
    }
    
    public void RegisterPlayer(int playerId, string playerName, TeamType team)
    {
        var playerData = new PlayerNetworkData
        {
            PlayerId = playerId,
            PlayerName = playerName,
            Team = team,
            JoinTime = Time.time,
            IsActive = true
        };
        
        connectedPlayers[playerId] = playerData;
        OnPlayerJoined?.Invoke(playerData);
    }
    
    public void RemovePlayer(int playerId)
    {
        if (connectedPlayers.ContainsKey(playerId))
        {
            connectedPlayers.Remove(playerId);
            OnPlayerLeft?.Invoke(playerId);
        }
    }
    
    private GameStateSnapshot CaptureGameState()
    {
        return new GameStateSnapshot
        {
            Tick = currentTick,
            Timestamp = Time.time,
            PlayerStates = new(connectedPlayers.Values)
        };
    }
    
    public PlayerNetworkData GetPlayer(int playerId)
    {
        return connectedPlayers.TryGetValue(playerId, out var player) ? player : null;
    }
    
    public List<PlayerNetworkData> GetPlayers(TeamType team)
    {
        var result = new List<PlayerNetworkData>();
        foreach (var player in connectedPlayers.Values)
        {
            if (player.Team == team)
                result.Add(player);
        }
        return result;
    }
}

public struct GameStateSnapshot
{
    public int Tick;
    public float Timestamp;
    public List<PlayerNetworkData> PlayerStates;
}

public class PlayerNetworkData
{
    public int PlayerId;
    public string PlayerName;
    public TeamType Team;
    public float JoinTime;
    public bool IsActive;
    public Vector3 LastKnownPosition;
    public int Gold;
    public int Level;
}
