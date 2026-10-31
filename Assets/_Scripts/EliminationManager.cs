using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class EliminationManager : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI OutcomeTmp;
    [SerializeField] private GameEvent OnElimination;
    [SerializeField] private SfxData WinSfx, LoseSfx;
    private ulong _id;
    private PlayerManager _playerManager;

    public override void OnNetworkSpawn()
    {
        _playerManager = PlayerManager.Instance;
        _id = NetworkManager.LocalClientId;
    }

    public void OnEliminationEvent(Component sender, object data)
    {
        var (loserId, conquerorId) = (Tuple<ulong, ulong>)data;
        if (!IsServer) return;
        OnEliminate(loserId);
    }

    public void OnSurrender()
    {
        SurrenderServerRpc(NetworkManager.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    private void SurrenderServerRpc(ulong defeatedId)
    {
        var ids = new Tuple<ulong, ulong>(defeatedId, ulong.MaxValue);
        OnElimination.Raise(ids);
    }

    private void OnEliminate(ulong loserId)
    {
        _playerManager.GetPlayerDataFromID(loserId).IsEliminated = true;
        ShowResultsRpc(false, loserId);
        
        var winnerTeamId = CheckWin();
        
        if (winnerTeamId != byte.MaxValue)
        {
            foreach (var player in _playerManager.Players)
            {
                if (!player.IsEliminated && player.TeamId.Value == winnerTeamId)
                {
                    ShowResultsRpc(true, player.Id.Value);
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShowResultsRpc(bool win, ulong playerId)
    {
        if (playerId == _id)
        {
            SelectionManager.Instance.DeSelectEverything();
            StartCoroutine(ShowOutcomeVfx(win));
        }
    }

    private IEnumerator ShowOutcomeVfx(bool win)
    {
        var outcomeTxt = win ? "VICTORY" : "DEFEAT";
        var sfx = win ? WinSfx : LoseSfx;
        OutcomeTmp.text = outcomeTxt;
        OutcomeTmp.gameObject.SetActive(true);
        SoundFXManager.Instance.PlaySfx(sfx, Vector3.zero);
        yield return new WaitForSeconds(3f);
        OutcomeTmp.gameObject.SetActive(false);
    }

    private byte CheckWin()
    {
        var aliveTeams = new HashSet<byte>();

        foreach (var player in _playerManager.Players)
        {
            if (player.IsEliminated) continue;
            aliveTeams.Add(player.TeamId.Value);

            if (aliveTeams.Count > 1)
                return byte.MaxValue;
        }

        return aliveTeams.Count == 1 ? aliveTeams.First() : byte.MaxValue;
    }
}