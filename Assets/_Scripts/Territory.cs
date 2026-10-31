using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Territory : NetworkBehaviour
{
    public NetworkVariable<uint> ID = new();
    private List<Node> _nodes = new();
    private Renderer _minimapRenderer;
    private readonly Dictionary<Node, GoldGenerator> _nodeGoldGenMap = new();
    private readonly Dictionary<Territory, MeshRenderer> _territoryRendererMap = new();
    private static List<Territory> _allTerritories  = new();
    
    [SerializeField] private Material SharedBorderMaterial;
    [SerializeField] private Material DefaultMidLineMaterial;
    [SerializeField] private SfxData TerritoryCaptureSfx;

    public void Init(List<Node> nodes)
    {
        _nodes = nodes;
        
        foreach (var node in _nodes)
        {
            _nodeGoldGenMap.Add(node, node.GetComponent<GoldGenerator>());
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            RequestDictServerRpc();
            return;
        }

        if (_nodes.Count > 0)
        {
            OnNodeCapture(null, _nodes[0].OwnerClientId);
        }

        MergeAllBorders();
    }

    [Rpc(SendTo.Server)]
    private void RequestDictServerRpc()
    {
        InitDictClientRpc();
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        HandleSfx(previous, current);
        if (IsServer) return;
        
        if (current == ulong.MaxValue)
        {
            UpdateAllBorders(SharedBorderMaterial);
        }
        else
        {
            UpdateAllBorders(GetBorderMaterial(false));
        }
        
        MergeAllBorders();
    }

    public void OnNodeCapture(Component sender, object data)
    {
        if (!IsServer) return;
        if (AllNodesHaveSameOwner())
        {
            OnCaptured();
        }
        else
        {
            OnLostControl();
        }

        MergeAllBorders();
    }

    private void HandleSfx(ulong prevId, ulong newId)
    {
        var clientId = NetworkManager.LocalClientId;
        if (newId == clientId && prevId != clientId)
        {
            SoundFXManager.Instance.PlaySfx(TerritoryCaptureSfx, Vector3.zero);
        }
    }

    private static void MergeAllBorders()
    {
        foreach (var territory in _allTerritories)
        {
            territory.MergeBorders();
        }
    }

    private void MergeBorders()
    {
        foreach (var (territory, meshRenderer) in _territoryRendererMap)
        {
            var territoryOwnerClientId = territory.ID.Value == 6969 ? 6969 : territory.OwnerClientId;
            UpdateBorderMaterials(meshRenderer, OwnerClientId == territoryOwnerClientId);
        }
    }

    private void UpdateBorderMaterials(MeshRenderer meshRenderer, bool isSameOwner)
    {
        var materials = meshRenderer.sharedMaterials.ToArray();
        materials[0] = isSameOwner ? GetBorderMaterial(true) : DefaultMidLineMaterial;
        materials[1] = isSameOwner || OwnerClientId == ulong.MaxValue ? SharedBorderMaterial : GetBorderMaterial(false);
        meshRenderer.sharedMaterials = materials;

    }
    
    private Material GetBorderMaterial(bool middle)
    {
        var color = GetColor();
        return OwnerClientId == ulong.MaxValue
            ? DefaultMidLineMaterial
            : middle
                ? color.MidBorderMaterial
                : color.BorderMaterial;
    }

    private Colors GetColor()
    {
        return ColorManager.Instance.Colors[PlayerManager.Instance.GetColorWithID(OwnerClientId)];
    }


    private bool _isGenerating;

    private void OnCaptured()
    {
        ChangeOwnership(_nodes[0].OwnerClientId);
        UpdateAllBorders(GetBorderMaterial(false));
        
        if (!_isGenerating)
        {
            StartCoroutine(nameof(TerritoryGoldGenerator));
            _isGenerating = true;
        }
    }

    private void OnLostControl()
    {
        StopAllCoroutines();
        _isGenerating = false;
        ChangeOwnership(ulong.MaxValue);
        UpdateAllBorders(SharedBorderMaterial);
    }

    private IEnumerator TerritoryGoldGenerator()
    {
        var wait = new WaitForSeconds(GoldManager.Instance.TerritoryGeneratorDelay);
        var increment = GoldManager.Instance.TerritoryGeneratorIncrement;
        while (true)
        {
            yield return wait;
            foreach (var kvp in _nodeGoldGenMap)
            {
                kvp.Value.AddGold(increment, true);
            }
        }
    }
    
    private void ChangeOwnership(ulong newOwnerId)
    {
        if (newOwnerId == NetworkObject.OwnerClientId) return;
        NetworkObject.ChangeOwnership(newOwnerId);
    }

    private void UpdateAllBorders(Material borderMaterial)
    {
        foreach (var meshRenderer in _territoryRendererMap.Values)
        {
            var materials = meshRenderer.sharedMaterials;
            materials[1] = borderMaterial;
            meshRenderer.sharedMaterials = materials;
        }

        if (_minimapRenderer.material)
        {
            _minimapRenderer.material = GetColor().MinimapMaterial;
        }
    }

    public void InitDict()
    {
        foreach (var minimapRenderer in GameObject.FindGameObjectWithTag("MinimapTerritories").GetComponentsInChildren<Renderer>())
        {
            if (minimapRenderer.name == "#MMT" + ID.Value)
            {
                _minimapRenderer = minimapRenderer;
            }
        }
        
        Dictionary<uint, Territory> idToTerritoryMap = new();
        _allTerritories = FindObjectsOfType<Territory>().ToList();
        
        foreach (var territory in _allTerritories)
        {
            idToTerritoryMap[territory.ID.Value] = territory;
        }

        foreach (var obj in GameObject.FindGameObjectWithTag("Borders").GetComponentsInChildren<Transform>())
        {
            var objName = obj.name;

            if (!objName.StartsWith("#B")) continue;

            var parts = objName.Substring(2).Split('_');
            if (parts.Length != 2) continue;

            if (!uint.TryParse(parts[0], result: out var a) || !uint.TryParse(parts[1], result: out var b)) continue;
            if (a != ID.Value) continue;

            if (!obj.TryGetComponent<MeshRenderer>(out var meshRenderer)) continue;

            if (idToTerritoryMap.TryGetValue(b, out var neighborTerritory))
            {
                _territoryRendererMap[neighborTerritory] = meshRenderer;
            }
            else if (b == 6969)
            {
                var waterT = obj.gameObject.AddComponent<Territory>();
                waterT.enabled = false;
                waterT.ID.Value = 6969;
                _territoryRendererMap[waterT] = meshRenderer;
            }
        }
    }

    [Rpc(SendTo.NotServer)]
    private void InitDictClientRpc()
    {
        InitDict();
        OnOwnershipChanged(OwnerClientId, OwnerClientId);
    }

    private bool AllNodesHaveSameOwner()
    {
        var firstOwner = _nodes.FirstOrDefault()?.OwnerClientId;
        return firstOwner.HasValue && _nodes.All(node => node.OwnerClientId == firstOwner);
    }
}