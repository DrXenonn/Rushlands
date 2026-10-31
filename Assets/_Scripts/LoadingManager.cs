using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LoadingManager : NetworkBehaviour
{
    [SerializeField] private GameObject LoadingScreen;
    [SerializeField] private GameEvent OnGameLoadedForEveryone;
    private byte _loadedPlayers;
    private bool _navMeshBaked;
    
    public void OnPlayerFullyLoad(Component sender, object data)
    {
        if (IsServer)
        {
            _loadedPlayers++;
            CheckForFullLoad();
        }
        else
        {
            ClientLoadServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void ClientLoadServerRpc()
    {
        _loadedPlayers++;
        CheckForFullLoad();
    }

    public void OnNavMeshFinishedBaking(Component sender, object data)
    {
        if (!IsServer) return;
        _navMeshBaked = true;
        CheckForFullLoad();
    }

    private void CheckForFullLoad()
    {
        if (!IsServer) return;
        if (_navMeshBaked && _loadedPlayers >= PlayerManager.Instance.Players.Count)
        {
            LoadRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void LoadRpc()
    {
        StartCoroutine(nameof(DelayLoad));
    }

    private IEnumerator DelayLoad()
    {
        GC.Collect();
        yield return new WaitForSeconds(1f);
        OnGameLoadedForEveryone.Raise();
        LoadingScreen.SetActive(false);
    }
}