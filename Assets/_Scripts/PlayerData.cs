using System;
using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    public NetworkVariable<ulong> Id = new();
    public NetworkVariable<FixedString128Bytes> Name = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<PlayerColor> Color;
    public NetworkVariable<byte> TeamId = new();
    public bool IsEliminated;

    public override void OnNetworkSpawn()
    {
        Id.Value = OwnerClientId;

        if (IsOwner)
        {
            SetPlayerNameServerRpc(SteamClient.Name);
            //SetPlayerNameServerRpc("P" + Id.Value);
        }

        PlayerManager.Instance.Players.Add(this);
        PlayerManager.Instance.PlayerIdTeamMap.Add(Id.Value, 1);
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerNameServerRpc(FixedString128Bytes playerName)
    {
        Name.Value = playerName;
    }

    public override void OnNetworkDespawn()
    {
        PlayerManager.Instance.Players.Remove(this);
        PlayerManager.Instance.PlayerIdTeamMap.Remove(Id.Value);
    }
}
