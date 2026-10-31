using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DefaultExecutionOrder(0)]
public class Node : NetworkBehaviour
{
    public TextMeshProUGUI UnitsText;
    public NodeSettings NodeSettings;
    [SerializeField] public NodeVisuals Visuals;
    [SerializeField] private SfxData UpgradeSfx;
    [SerializeField] private ParticleSystem UpgradeVfx;
    [SerializeField] private Image SelectedVisuals;
    [SerializeField] private GameEvent OnNodeSpawn;
    [SerializeField] private GameEvent OnCaptureEvent;
    [SerializeField] private MeshRenderer[] Renderers;
    [SerializeField] private Ballista BallistaPrefab;
    [SerializeField] private GoldGenerator GoldGenerator;
    [SerializeField] private LineRenderer LineRenderer;
    [SerializeField] private GameObject OverpopSign;
    [SerializeField] private Animator UnitTextAnimator;
    [SerializeField] private Image MinimapIcon;
    [SerializeField] private Transform CamPosition;

    [FormerlySerializedAs("OnStrongholdCaptured")] [SerializeField]
    private GameEvent OnElimination;

    public NetworkVariable<int> CurrentUnits = new(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> OwnerID = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [FormerlySerializedAs("Color")] public PlayerColor Color = PlayerColor.None;

    public NodeSettings RunTimeSettings;
    private WaitForSeconds _waitForGenDelay, _waitForOverPopDelay;
    private bool _selected;

    private void ResetUnitGenCoroutine()
    {
        StopAllCoroutines();

        _waitForGenDelay = new WaitForSeconds(RunTimeSettings.GetUnitGenDelay());
        _waitForOverPopDelay = new WaitForSeconds(1.5f);

        StartCoroutine(GenerateUnits());
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UpgradeRpc((byte)NodeUpgradeManager.Instance.NodeSettingsMap.IndexOf(NodeSettings), false);
        }

        OnNodeSpawn.Raise(transform, this);

        if (!IsServer)
        {
            enabled = false;
        }

        CurrentUnits.OnValueChanged += UpdateUI;
        OwnerID.OnValueChanged += UpdateColors;

        if (IsServer)
        {
            CurrentUnits.Value = NodeSettings.StartingUnits;
            ResetUnitGenCoroutine();
        }
        else
        {
            UnitsText.text = CurrentUnits.Value.ToString();
        }

        OwnerID.Value = OwnerClientId;
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (IsServer)
        {
            OwnerID.Value = current;
        }
    }

    private void UpdateColors(ulong previous, ulong current)
    {
        var playerColor = PlayerManager.Instance.GetColorWithID(current);
        Color = playerColor;
        var color = ColorManager.Instance.Colors[playerColor];
        UnitsText.color = color.UnitTextColor;
        SelectedVisuals.color = color.SelectionHighlightColor;
        LineRenderer.startColor = color.SelectionHighlightColor;
        LineRenderer.endColor = color.SelectionHighlightColor;
        MinimapIcon.color = color.UnitTextColor;
            
        foreach (var r in Renderers)
        {
            r.material = color.Material;
        }
    }

    private void UpdateUI(int previousValue, int newValue)
    {
        UnitsText.text = newValue.ToString();
        OverpopSign.SetActive(newValue > RunTimeSettings.MaxUnits);
        if (newValue - previousValue == 1) return;
        var animationName = newValue > previousValue ? "NodeUnitCountIncrease" : "NodeUnitCountDecrease";
        UnitTextAnimator.Play(animationName);
    }

    private IEnumerator GenerateUnits()
    {
        yield return new WaitForSeconds(5f);
        while (true)
        {
            var isOn = RunTimeSettings.GetUnitGenDelay() > 0;
            if (CurrentUnits.Value < RunTimeSettings.MaxUnits && isOn)
            {
                yield return _waitForGenDelay;
                CurrentUnits.Value++;
            }
            else if (CurrentUnits.Value > RunTimeSettings.MaxUnits)
            {
                CurrentUnits.Value--;
                yield return _waitForOverPopDelay;
            }
            else
            {
                yield return null;
            }
        }
        // ReSharper disable once IteratorNeverReturns
    }

    public void Trade(PlayerColor traderColor, int unitCount)
    {
        if (unitCount <= 0) return;
        var playerManager = PlayerManager.Instance;
        var playerTeamMap = playerManager.PlayerIdTeamMap;
        var traderId = playerManager.GetIDWithColor(traderColor);
        var traderTeam = playerTeamMap[traderId];
        var nodeTeam = playerTeamMap[OwnerClientId];
        var onTheSameTeam = nodeTeam == traderTeam && nodeTeam != 0;

        if (traderColor == Color || onTheSameTeam)
        {
            CurrentUnits.Value += unitCount;
        }
        else
        {
            var damage = Math.Max(0, unitCount - RunTimeSettings.Armor);

            if (CurrentUnits.Value >= damage)
            {
                CurrentUnits.Value -= damage;
            }
            else
            {
                var residue = damage - CurrentUnits.Value;
                CurrentUnits.Value = 0;
                OnCapture(traderId);
                CurrentUnits.Value = residue;
            }
        }
    }

    private void OnCapture(ulong newOwnerId)
    {
        if (!IsServer) return;
        DeselectCapturedRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
        
        if (NodeSettings.IsMainBuilding)
        {
            var ids = new Tuple<ulong, ulong>(OwnerClientId, newOwnerId);
            OnElimination.Raise(ids);
        }
        else
        {
            NetworkObject.ChangeOwnership(newOwnerId);
            OnCaptureEvent.Raise(this, NetworkObject.OwnerClientId);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void DeselectCapturedRpc(RpcParams rpcParams)
    {
        if (_selected)
        {
            SelectionManager.Instance.DeSelect(this);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UpgradeRpc(byte newSettingsId, bool playSfx = true)
    {
        var newSettings = NodeUpgradeManager.Instance.NodeSettingsMap[newSettingsId];

        if (Visuals != null)
        {
            StartCoroutine(DestroyAndContinue(Visuals.gameObject, newSettings, playSfx));
        }
        else
        {
            ApplyNewSettings(newSettings, playSfx);
        }
    }

    private IEnumerator DestroyAndContinue(GameObject obj, NodeSettings newSettings, bool playSfx = true)
    {
        Destroy(obj);

        while (obj != null)
        {
            yield return null;
        }

        ApplyNewSettings(newSettings, playSfx);
    }

    private void ApplyNewSettings(NodeSettings newSettings, bool playSfx = true)
    {
        NodeSettings = newSettings;
        RunTimeSettings = newSettings;

        if (IsServer)
        {
            ResetUnitGenCoroutine();
            GoldGenerator.SwitchGen(true);
        }

        UpdateVisuals(newSettings, playSfx);
    }

    private void UpdateVisuals(NodeSettings nodeSettings, bool playSfx = true)
    {
        Visuals = Instantiate(nodeSettings.NodeVisuals, transform);

        if (IsServer && nodeSettings.IsBallista)
        {
            var ballista = Instantiate(BallistaPrefab, Visuals.GetBallistaTransform());
            ballista.Node = this;
            ballista.NetworkObject.Spawn();
        }

        Renderers = Visuals.GetRenderers();
        UnitsText.transform.parent.position = Visuals.GetNodeCountPos();
        SelectedVisuals = Visuals.GetSelectionHighlightImage();

        UpdateColors(0, OwnerClientId);

        if (_selected)
        {
            OnSelect();
        }

        UpdateUI(-1, CurrentUnits.Value);

        if (IsOwner)
        {
            NodeUIManager.Instance.OnFinishUpgrade();
        }

        UpgradeVfx.Play();

        if (playSfx) SoundFXManager.Instance.PlaySfx(UpgradeSfx, transform.position);
        
        if (NodeSettings.IsMainBuilding && NetworkManager.LocalClientId == OwnerClientId)
        {
            var cam = Camera.main.transform;
            var camContainer = cam.parent;

            var offset = cam.position - camContainer.position;

            camContainer.position = new Vector3(
                CamPosition.position.x - offset.x,
                camContainer.position.y,
                CamPosition.position.z - offset.z
            );
        }
    }

    public void OnBallistaSpawn(Component sender, object data)
    {
        var ballistasNode = (Node)sender;
        var ballista = (Ballista)data;
        if (ballistasNode != this) return;

        if (IsServer)
        {
            ballista.Init();
        }

        ballista.NetworkObject.TrySetParent(Visuals.GetBallistaTransform());
        ballista.transform.localRotation = Quaternion.Euler(0, 0, 180);
        ballista.transform.localPosition = Vector3.zero;
    }

    public void OnEliminate(Component sender, object data)
    {
        if (!IsServer) return;
        var (loserId, conquerorId) = (Tuple<ulong, ulong>)data;
        if (loserId == OwnerClientId)
        {
            var newOwnerId = RunTimeSettings.IsMainBuilding ? conquerorId : ulong.MaxValue;
            NetworkObject.ChangeOwnership(newOwnerId);
            OnCaptureEvent.Raise(this, NetworkObject.OwnerClientId);

            if (RunTimeSettings.IsMainBuilding)
            {
                UpgradeRpc(0, false);
            }
        }
    }

    public void UpdateDeploymentLr(Vector3[] points)
    {
        LineRenderer.SetPositions(points);
    }

    public void UpdateDeploymentLr(bool state)
    {
        if (LineRenderer.enabled == state) return;
        LineRenderer.enabled = state;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        CurrentUnits.OnValueChanged -= UpdateUI;
    }

    public void OnSelect()
    {
        SelectedVisuals.gameObject.SetActive(true);
        _selected = true;
    }

    public void OnDeSelect()
    {
        SelectedVisuals.gameObject.SetActive(false);
        _selected = false;
    }

    public bool IsNodeOwner()
    {
        return IsOwner;
    }
}