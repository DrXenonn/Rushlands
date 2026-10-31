using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerList : NetworkBehaviour
{
    [SerializeField] private Transform NamesParent;
    [SerializeField] private TextMeshProUGUI PlayerName;
    private Dictionary<ulong, TextMeshProUGUI> _playerIdNameMap = new();

    public override void OnNetworkSpawn()
    {
        var playerManager = PlayerManager.Instance;
        foreach (var player in playerManager.Players)
        {
            var playerId = player.Id;
            var team = player.TeamId.Value;
            var teamTxt = team.ToString();
            var color = playerManager.GetColorWithID(playerId.Value);
            var uiColor = ColorManager.Instance.Colors[color].UIColor;

            var playerNameTMP = Instantiate(PlayerName, NamesParent);
            var playerIcon = playerNameTMP.GetComponentInChildren<Image>();
            playerNameTMP.color = uiColor;
            playerIcon.color = uiColor;
            playerNameTMP.text = player.Name.Value + " [" + teamTxt + "]";
            _playerIdNameMap.Add(playerId.Value, playerNameTMP);
        }
    }

    public void OnPlayerEliminate(Component sender, object data)
    {
        var (loserId, conquerorId) = (Tuple<ulong, ulong>)data;
        _playerIdNameMap[loserId].fontStyle = FontStyles.Strikethrough;
        OnEliminateClientRpc(loserId);
    }

    [Rpc(SendTo.NotServer)]
    private void OnEliminateClientRpc(ulong loserId)
    {
        _playerIdNameMap[loserId].fontStyle = FontStyles.Strikethrough;
    }
}