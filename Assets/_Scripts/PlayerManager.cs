using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public List<PlayerData> Players = new();
    public Transform PlayerListManager;
    public readonly Dictionary<ulong, byte> PlayerIdTeamMap = new();
    [SerializeField] private GameEvent OnPlayerColorChanged;
    [SerializeField] private GameEvent OnNetworkSpawned;
    
    public List<PlayerColor> AvailableColors = new(6)
    {
        PlayerColor.Blue,
        PlayerColor.Red,
        PlayerColor.Yellow,
        PlayerColor.Green,
        PlayerColor.Orange,
        PlayerColor.Purple,
    };

    public override void OnNetworkSpawn()
    {
        Invoke(nameof(CallNetworkSpawnEvent), 0.25f);
        PlayerIdTeamMap.Add(ulong.MaxValue, byte.MaxValue);

        if (!IsServer)
        {
            enabled = false;
        }
    }

    private void CallNetworkSpawnEvent()
    {
        OnNetworkSpawned.Raise();
    }

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

    public PlayerColor GetColorToAssign(ulong playerID, PlayerColor prevColor)
    {
        var colorToReturn = AvailableColors[0];
        AvailableColors.RemoveAt(0);

        if (!AvailableColors.Contains(prevColor) && prevColor != PlayerColor.None)
        {
            AvailableColors.Add(prevColor);
        }

        var player = GetPlayerDataFromID(playerID);
        player.Color.Value = colorToReturn;

        if (prevColor != PlayerColor.None)
        {
            var colors = new Tuple<PlayerColor, PlayerColor>(prevColor, colorToReturn);
            OnPlayerColorChanged.Raise(colors);
        }
        
        return colorToReturn;
    }
    
    public PlayerColor GetColorWithID(ulong id)
    {
        if (id == ulong.MaxValue) return PlayerColor.None;
        return Players.FirstOrDefault(data => data.Id.Value == id)!.Color.Value;
    }

    public void AssignPlayerTeam(ulong playerId, byte teamId)
    {
        var playerData = Players.FirstOrDefault(player => player.Id.Value == playerId);
        if (playerData == null) return;
        playerData.TeamId.Value = teamId;
        PlayerIdTeamMap[playerData.Id.Value] = teamId;
    }

    public ulong GetIDWithColor(PlayerColor color)
    {
        if (color == PlayerColor.None) return ulong.MaxValue;
        return Players.FirstOrDefault(data => data.Color.Value == color)!.Id.Value;
    }

    public PlayerData GetPlayerDataFromID(ulong id)
    {
        return Players.FirstOrDefault(player => player.Id.Value == id);
    }

    public FixedString128Bytes GetPlayerName(ulong id)
    {
        return Players.FirstOrDefault(player => player.Id.Value == id)?.Name.Value.ToString();
    }
}