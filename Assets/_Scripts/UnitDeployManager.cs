using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public struct UnitGroupData
{
    public Transform Target;
    public PlayerColor Color;
    public int Count;
}

public class UnitDeployManager : NetworkBehaviour
{
    [SerializeField] private Camera Cam;
    [SerializeField] private SelectionManager SelectionManager;
    [SerializeField] private LayerMask TargetableLayers;
    [SerializeField] private UnitGroup UnitGroup;
    [SerializeField] private GameObject Progressbar;
    [SerializeField] private Image ProgressbarImage;
    [SerializeField] private TextMeshProUGUI ProgressbarText;
    [SerializeField] private SfxData DeploySfx;
    [SerializeField] private short DeployGoldCost = 5;
    [SerializeField] private float DynamicDeploymentSpeed = 20f;
    private float _dynamicDeploymentPercentage;
    private SoundFXManager _soundFXManager; 

    private readonly Dictionary<ulong, Node> _nodes = new();
    private readonly Dictionary<Node, ulong> _reverseNodes = new();

    private void Start()
    {
        _soundFXManager = SoundFXManager.Instance;
    }

    public override void OnNetworkSpawn()
    {
        ProgressbarImage.color = ColorManager.Instance
            .Colors[PlayerManager.Instance.GetColorWithID(NetworkManager.LocalClientId)].UIColor;
    }

    public void OnNodeSpawned(Component sender, object data)
    {
        var id = sender.GetComponent<NetworkObject>().NetworkObjectId;
        var node = (Node)data;

        if (_nodes.TryAdd(id, node))
        {
            _reverseNodes[node] = id;
        }
    }

    public void OnRightClick(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled)
        {
            OnRightClickUp();
            OnCancelHold();
        }
        else if (ctx.performed)
        {
            OnHold();
        }
    }

    private bool _isHolding;
    public void OnHold()
    {
        if (!Physics.Raycast(Cam.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity,
                TargetableLayers) || SelectionManager.SelectedNodes.Count < 1) return;
        Progressbar.SetActive(true);
        _isHolding = true;
        StartCoroutine(nameof(FillBar));
    }

    private void OnCancelHold()
    {
        if (!_isHolding) return;

        OnRightClickUp();
        StopAllCoroutines();
        Progressbar.SetActive(false);
        _dynamicDeploymentPercentage = 0;
        SelectionManager.DeSelectEverything();
        
        _isHolding = false;
    }

    private IEnumerator FillBar()
    {
        while (_dynamicDeploymentPercentage < 100f && Physics.Raycast(Cam.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity,TargetableLayers) && SelectionManager.SelectedNodes.Count > 0)
        {
            Progressbar.transform.position = new Vector2(Input.mousePosition.x + 30, Input.mousePosition.y + 60);
            _dynamicDeploymentPercentage += DynamicDeploymentSpeed * Time.deltaTime;
            ProgressbarImage.fillAmount = _dynamicDeploymentPercentage / 100;
            
            var sum = SelectionManager.SelectedNodes.Sum(selected => selected.CurrentUnits.Value);
            
            ProgressbarText.text = Mathf.FloorToInt(sum * _dynamicDeploymentPercentage / 100).ToString(); 
                
            yield return null;
        }

        OnCancelHold();
    }
    
    private void OnRightClickUp()
    {
        if (!Physics.Raycast(Cam.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity,
                TargetableLayers) || SelectionManager.SelectedNodes.Count < 1) return;

        var clientId = NetworkManager.LocalClientId;
        var targetNode = hit.collider.GetComponent<Node>();
        var constantPercent = Keyboard.current.ctrlKey.isPressed ? 100f : 50f;
        var percentage = _isHolding ? (byte)Mathf.RoundToInt(_dynamicDeploymentPercentage) : (byte)constantPercent;
        
        if (!IsServer)
        {
            var sent = false;
            foreach (var node in SelectionManager.SelectedNodes)
            {
                if (_reverseNodes.TryGetValue(node, out var nodeId) &&
                    _reverseNodes.TryGetValue(targetNode, out var targetNodeId))
                {
                    var success = (node.CurrentUnits.Value > 0 && GoldManager.Instance.GetGold(clientId) > 0);
                    if (success) sent = true;
                    DeployUnitsRpc(nodeId, targetNodeId, percentage, clientId);
                }
            }

            if (sent) _soundFXManager.PlaySfx(DeploySfx, Vector3.zero);
        }
        else
        {
            var sent = false;
    
            foreach (var node in SelectionManager.SelectedNodes)
            {
                if (node == targetNode) continue;
        
                var success = DeployUnits(node, targetNode, percentage);
                if (success) sent = true;
            }

            if (sent) _soundFXManager.PlaySfx(DeploySfx, Vector3.zero);
        }


        SelectionManager.DeSelectEverything();
    }

    private bool DeployUnits(Node selectedNode, Node targetNode, byte percentage = 50)
    {
        var unitCount = Mathf.FloorToInt(selectedNode.CurrentUnits.Value * (percentage / 100f));
        if (unitCount < 1) return false;
        
        var hasGold = GoldManager.Instance.UpdateGold(selectedNode.OwnerClientId, (short)-DeployGoldCost);
        if (!hasGold) return false;
        
        selectedNode.CurrentUnits.Value -= unitCount;
        var unitData = new UnitGroupData
        {
            Color = selectedNode.Color,
            Count = unitCount,
            Target = targetNode.transform,
        };

        var nodePos = selectedNode.transform.position;
        var direction = (targetNode.transform.position - nodePos).normalized;
        var spawnPos = (nodePos + (direction * 5));
        var unitGroup = Instantiate(UnitGroup, spawnPos, quaternion.identity);
        unitGroup.Init(unitData);
        unitGroup.NetworkObject.Spawn();
        return true;
    }
    
    [Rpc(SendTo.Server)]
    private void DeployUnitsRpc(ulong nodeId, ulong targetNodeId, byte percentage, ulong senderId)
    {
        if(_nodes[nodeId].OwnerClientId != senderId) return;
        DeployUnits(_nodes[nodeId], _nodes[targetNodeId], percentage);
    }
}