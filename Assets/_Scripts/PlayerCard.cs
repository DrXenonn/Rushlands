using Steamworks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCard : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI PlayerNameUI;
    [SerializeField] private Image PlayerColorUI;
    [SerializeField] private TextMeshProUGUI PlayerTeamUI;
    [SerializeField] private int MaxTeamNumber;
    private bool _firstColorChange = true;

    private readonly NetworkVariable<PlayerColor> _playerColor =
        new NetworkVariable<PlayerColor>(writePerm: NetworkVariableWritePermission.Server,
            readPerm: NetworkVariableReadPermission.Everyone);

    private readonly NetworkVariable<int> _playerTeam =
        new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner,
            readPerm: NetworkVariableReadPermission.Everyone);

    private readonly NetworkVariable<FixedString128Bytes> _playerName =
        new(writePerm: NetworkVariableWritePermission.Server,
            readPerm: NetworkVariableReadPermission.Everyone);
    
    public override void OnNetworkSpawn()
    {
        _playerTeam.OnValueChanged += UpdatePlayerTeamUI;
        _playerColor.OnValueChanged += UpdatePlayerColorUI;
        _playerName.OnValueChanged += UpdatePlayerNameUI;

        transform.SetParent(PlayerManager.Instance.PlayerListManager!, false);
        
        InitValues();
        if (IsOwner)
        {
            RequestColorRpc();
            RequestPlayerNameRpc();
        }
    }

    private void InitValues()
    {
        PlayerNameUI.text = _playerName.Value.ToString();
        var txt = _playerTeam.Value.ToString();
        PlayerTeamUI.text = txt;
        ChangeColor(_playerColor.Value);
    }
    
    private void UpdatePlayerNameUI(FixedString128Bytes previousvalue, FixedString128Bytes newvalue)
    {
        PlayerNameUI.text = newvalue.ToString();
    }

    private void UpdatePlayerTeamUI(int previousvalue, int newvalue)
    {
        var txt = _playerTeam.Value.ToString();
        PlayerTeamUI.text = txt;
    }

    private void UpdatePlayerColorUI(PlayerColor previousvalue, PlayerColor newvalue)
    {
        ChangeColor(newvalue);
        
        if (IsOwner && _firstColorChange)
        {
            _playerTeam.Value = (int)newvalue;
            SetTeamServerRpc((byte)_playerTeam.Value);
            _firstColorChange = false;
        }
    }

    private void ChangeColor(PlayerColor playerColor)
    {
        var color = ColorManager.Instance.Colors[playerColor].UIColor;
        PlayerColorUI.color = color;
        PlayerNameUI.color = color;
    }

    public void OnTeamChange()
    {
        if (!IsOwner) return;
        var currentTeam = _playerTeam.Value;
        _playerTeam.Value = currentTeam == MaxTeamNumber ? 1 : currentTeam + 1;
        SetTeamServerRpc((byte)_playerTeam.Value);
    }

    public void OnColorChange()
    {
        if (!IsOwner) return;
        RequestColorRpc();
    }

    [Rpc(SendTo.Server)]
    private void SetTeamServerRpc(byte teamId)
    {
        PlayerManager.Instance.AssignPlayerTeam(OwnerClientId, teamId);
    }

    [Rpc(SendTo.Server)]
    private void RequestColorRpc()
    {
        _playerColor.Value = PlayerManager.Instance.GetColorToAssign(OwnerClientId, _playerColor.Value);
    }

    [Rpc(SendTo.Server)]
    private void RequestPlayerNameRpc()
    {
        _playerName.Value = PlayerManager.Instance.GetPlayerName(OwnerClientId);
    }

    public override void OnDestroy()
    {
        _playerTeam.OnValueChanged -= UpdatePlayerTeamUI;
        _playerColor.OnValueChanged -= UpdatePlayerColorUI;
    }
}