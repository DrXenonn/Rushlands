using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct Map
{
    public List<MapNodeData> Nodes;
}

public class MapEditor : MonoBehaviour
{
    [SerializeField] private NetworkObject TerritoryNode, WaterNode;
    [SerializeField] private MapHandler MapHandler;
    [SerializeField] private GameObject ChooseMapButton;
    [SerializeField] private List<Map> Maps = new();
    [SerializeField] private List<MapNode> MapNodes = new();
    
    public void OnNetworkSpawn(Component sender, object data)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        ChooseMapButton.SetActive(true);
    }

    public void OnMapClick(int index)
    {
        DeSpawnAllMapNodes();
        
        foreach (var node in Maps[index].Nodes)
        {
            var nodeType = node.IsWater ? WaterNode : TerritoryNode;
            var spawnedNode = SpawnNode(nodeType).GetComponent<MapNode>();
            spawnedNode.BuildingNodesCount.Value = node.BuildingNodeCount;
            spawnedNode.GetComponent<RectTransform>().localPosition = node.Position;
            spawnedNode.MinimumSize.Value = node.MinSize;
        }
    }
    
/*#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            var map = new Map
            {
                Nodes = new List<MapNodeData>()
            };
            foreach (var node in MapNodes)
            {
                if (node == null) continue;
                var data = new MapNodeData()
                {
                    BuildingNodeCount = node.BuildingNodesCount.Value,
                    Position = node.GetComponent<RectTransform>().localPosition,
                    IsWater = node.IsWater,
                    MinSize = node.MinimumSize.Value,
                };
                map.Nodes.Add(data);
            }
            Maps.Add(map);
        }
    }
#endif*/

    public void OnClick()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        NetworkObject node;
        
        if (Keyboard.current.aKey.isPressed)
        {
            node = TerritoryNode;
        }
        else if (Keyboard.current.wKey.isPressed)
        {
            node = WaterNode;
        }
        else
        {
            return;
        }
        
        SpawnNode(node);
    }

    private NetworkObject SpawnNode(NetworkObject node)
    {
        var spawnedNode = Instantiate(node, Mouse.current.position.value, quaternion.identity, transform);
        spawnedNode.Spawn();
        MapNodes.Add(spawnedNode.GetComponent<MapNode>());
        return spawnedNode;
    }

    private void DeSpawnAllMapNodes()
    {
        foreach (var mapNode in MapNodes.ToList())
        {
            if (mapNode == null) continue;
            mapNode.GetComponent<NetworkObject>().Despawn();
            Destroy(mapNode.gameObject);
        }
        MapNodes.Clear();
    }

    public void OnHostStartButtonClicked(Component sender, object data)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        for (var i = 0; i < transform.childCount; i++)
        {
            var mapNode = transform.GetChild(i).GetComponent<MapNode>();
            var nodeData = new MapNodeData
            {
                Color = mapNode.Color.Value,
                Position = mapNode.GetComponent<RectTransform>().localPosition,
                BuildingNodeCount = mapNode.BuildingNodesCount.Value,
                IsWater = mapNode.IsWater,
                MinSize = mapNode.MinimumSize.Value,
            };
            MapHandler.MapNodes.Add(nodeData);
        }
    }
}