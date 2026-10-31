using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[System.Serializable]
public struct MapNodeData
{
    public Vector3 Position;
    [FormerlySerializedAs("Color")] public PlayerColor Color;
    public int BuildingNodeCount;
    public bool IsWater;
    public int MinSize;
}

public class MapHandler : NetworkBehaviour
{
    public static MapHandler Instance { get; private set; }
    [SerializeField] private GameEvent OnGameSceneLoad;
    public List<MapNodeData> MapNodes = new List<MapNodeData>();
    public GameEvent OnShare;
    private int _mapGenCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.SceneManager.OnLoadComplete += OnSceneLoad;
    }

    private void OnSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName != "Game") return;

        if (clientId == NetworkManager.LocalClientId)
        {
            OnGameSceneLoad.Raise();
        }
    }

    [Rpc(SendTo.Server)]
    public void IncreaseMapGenCountRpc()
    {
        _mapGenCount++;
        if (_finished)
        {
            TrySharing();
        }
    }

    private bool _finished;
    public void OnFinishMapGeneration(Component sender, object data)
    {
        if (!IsServer) return;
        _finished = true;
        TrySharing();
    }
    
    private void TrySharing()
    {
        if (_mapGenCount >= (PlayerManager.Instance.Players.Count))
        {
            OnShare.Raise();
        }
    }
    
    public override void OnNetworkDespawn()
    {
        NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoad;
    }
}