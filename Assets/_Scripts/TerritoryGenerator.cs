using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[Serializable]
public class TerritoryData
{
    public uint Id;
    public int BuildingNodesCount;
    public int MinimumSize = 1;
    public PlayerColor PlayerColor;
    public Territory TerritoryComponent;
    public Vector3 SeedPosition;
    public List<Hex> Hexes = new();
    public HashSet<Hex> PossibleHexes = new();
    public List<Hex> PossibleNodeHexes = new();
    public List<Node> SpawnedNodes = new();
    public Dictionary<uint, MeshRenderer> NeighbourIdRendererMap = new();
}

[Serializable]
public struct MapColor
{
    public Material[] Materials;
}

public class TerritoryGenerator : NetworkBehaviour
{
    #region Variables

    [SerializeField] private List<Hex> AllHexes = new List<Hex>();
    [SerializeField] private List<TerritoryData> Territories = new List<TerritoryData>();
    [SerializeField] private Node Node;
    [SerializeField] private NodeSettings TentSettings;
    [SerializeField] private NodeSettings StrongholdSettings;
    [SerializeField] private NodeSettings ShipSettings;
    [SerializeField] private GameObject Waypoint;
    [SerializeField] private MapColor[] MapColors;
    [SerializeField] private GameEvent OnFinishMapGeneration;
    [SerializeField] private GameEvent OnNavMeshFinishedBaking;
    [SerializeField] private LayerMask HexLayerMask;
    [SerializeField] private Territory TerritoryObject;
    [SerializeField] private NavMeshSurface MapNavMeshSurface;
    [SerializeField] private NavMeshData MapNavMeshData;
    [SerializeField] private MeshRenderer Edge;
    [SerializeField] private GameEvent OnGameFullyLoad;
    [SerializeField] private Transform[] DetailPrefabs;
    [SerializeField] private float WaterLevel = -0.25f;
    [SerializeField] private float NodesMinDistanceInRadius;
    [SerializeField] private uint WaterID;
    
    #endregion

    public IEnumerator Generate()
    {
        
        Stopwatch s = new Stopwatch();
        s.Start();
        
        InitializeNodesData();
        yield return null;
        print("InitNodesData: " + s.ElapsedMilliseconds);
        s.Restart();

        InitializeSeedHexes();
        yield return null;
        print("InitializeSeedHexes: " + s.ElapsedMilliseconds);
        s.Restart();

        GenerateTerritories();
        yield return null;
        print("GenerateTerritories: " + s.ElapsedMilliseconds);
        s.Restart();

        SpawnNodes();
        yield return null;
        print("SpawnNodes: " + s.ElapsedMilliseconds);
        s.Restart();
        
        UpdateMapColors();
        yield return null;
        print("UpdateMapColors: " + s.ElapsedMilliseconds);
        s.Stop();

        OnFinishMapGeneration.Raise();
    }

    public void OnHexSpawn(Component sender, object data)
    {
        AllHexes.Add((Hex)sender);
    }

    private void InitializeNodesData()
    {
        uint index = 1;

        float uiXMin = -513, uiXMax = 513;
        float uiYMin = -354, uiYMax = 354;

        float worldXMin = -58f, worldXMax = 90.5f;
        float worldZMin = -59.2f, worldZMax = 147.7801f;

        foreach (var nodeData in MapHandler.Instance.MapNodes)
        {
            var territory = new TerritoryData();
            territory.Id = nodeData.IsWater ? WaterID : index++;

            var worldZ = Mathf.Lerp(worldZMin, worldZMax, (nodeData.Position.x - uiXMin) / (uiXMax - uiXMin));

            var worldX = Mathf.Lerp(worldXMin, worldXMax, (uiYMax - nodeData.Position.y) / (uiYMax - uiYMin));

            var worldPosition = new Vector3(worldX, 0f, worldZ);

            territory.SeedPosition = worldPosition;
            territory.BuildingNodesCount = nodeData.BuildingNodeCount;
            territory.PlayerColor = nodeData.Color;
            territory.MinimumSize = nodeData.MinSize;
            Territories.Add(territory);
        }
    }

    private void GenerateTerritories()
    {
        while (true)
        {
            var doneTerritories = 0;

            foreach (var territory in Territories)
            {
                territory.PossibleHexes.RemoveWhere(possibleHex => possibleHex.Id != 0);

                if (territory.PossibleHexes.Count > 0)
                {
                    var randomIndex = Random.Range(0, territory.PossibleHexes.Count);
                    UpdateTerritoryHexes(territory, territory.PossibleHexes.ElementAt(randomIndex),
                        Territories.IndexOf(territory));
                }
                else
                {
                    doneTerritories++;
                }
            }

            if (doneTerritories >= Territories.Count)
            {
                break;
            }
        }

        AddEdges();
    }
    
    private void AddEdges()
    {
        var borders = new GameObject { name = "Borders" };
        borders.tag = "Borders";
        foreach (var territory in Territories)
        {
            Dictionary<uint, List<MeshRenderer>> dict = new();
            foreach (var hex in territory.Hexes)
            {
                if (hex.NeighborHexes.Count < 6) continue;
                for (var i = 0; i < hex.NeighborHexes.Count; i++)
                {
                    var neighbourId = hex.NeighborHexes[i].Id;
                    
                    if (neighbourId == hex.Id) continue;
                    hex.IsOnEdge = true;
                    if (territory.Id == WaterID) continue;
                    
                    var rot = i switch
                    {
                        0 => new Vector3(-30, 90, 90),
                        1 => new Vector3(30, 90, 90),
                        2 => new Vector3(90, 0, 0),
                        3 => new Vector3(30, -90, -90),
                        4 => new Vector3(-30, -90, -90),
                        5 => new Vector3(-90, 0, -180),
                        _ => Vector3.zero
                    };
                    var position = new Vector3(0, 0, 0.00005f);

                    var e = Instantiate(Edge, hex.transform);
            
                    e.transform.localPosition = position;
                    e.transform.localRotation = Quaternion.Euler(rot);
                    
                    if (!dict.ContainsKey(neighbourId))
                    {
                        dict[neighbourId] = new List<MeshRenderer>();
                    }

                    dict[neighbourId].Add(e);
                }
            }

            foreach (var kvp in dict)
            {
                var border = new GameObject
                {
                    name = "#B" + territory.Id + "_" + kvp.Key,
                };
                
                foreach (var r in kvp.Value)
                {
                    r.transform.SetParent(border.transform);
                }
                
                CombineMeshes(border.transform, true);
                border.transform.SetParent(borders.transform);
                territory.NeighbourIdRendererMap.Add(kvp.Key, border.GetComponent<MeshRenderer>());
            }
        }

        var minimapBorders = Instantiate(borders);
        minimapBorders.layer = LayerMask.NameToLayer("Border");
        CombineMeshes(minimapBorders.transform, true);
    }
    
    private void InitializeSeedHexes()
    {
        foreach (var territory in Territories)
        {
            Hex closestHex = null;
            var shortestDistance = float.MaxValue;

            foreach (var hex in AllHexes)
            {
                var distance =
                    Vector3.SqrMagnitude(hex.transform.position -
                                         territory.SeedPosition); // Use SqrMagnitude for performance

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestHex = hex;
                }
            }

            var hexesToInit = GetExpandedHexes(closestHex, territory.MinimumSize);
            foreach (var hex in hexesToInit)
            {
                UpdateTerritoryHexes(territory, hex, Territories.IndexOf(territory));
            }
        }
    }

    private HashSet<Hex> GetExpandedHexes(Hex startHex, int iterations)
    {
        var result = new HashSet<Hex>();
        var queue = new Queue<(Hex hex, int depth)>();
        queue.Enqueue((startHex, 0));

        while (queue.Count > 0)
        {
            var (currentHex, depth) = queue.Dequeue();

            // Stop expanding after reaching max iterations
            if (depth >= iterations) continue;

            foreach (var neighbor in currentHex.NeighborHexes)
            {
                if (result.Add(neighbor)) // Ensures we don’t add duplicates
                    queue.Enqueue((neighbor, depth + 1));
            }
        }

        return result;
    }


    private void UpdateTerritoryHexes(TerritoryData t, Hex h, int tIndex)
    {
        t.Hexes.Add(h);
        h.Id = t.Id;
        h.TerritoryIndex = tIndex;
        t.PossibleHexes.UnionWith(h.NeighborHexes);

        if (t.PossibleHexes.Contains(h))
        {
            t.PossibleHexes.Remove(h);
        }
    }

    private void SpawnNodes()
    {
        foreach (var territory in Territories)
        {
            if (territory.Id == WaterID && territory.BuildingNodesCount > 1)
            {
                var wanderingShip = Instantiate(Node, territory.SeedPosition, Quaternion.identity);
                wanderingShip.NodeSettings = ShipSettings;
                wanderingShip.NetworkObject.SpawnWithOwnership(ulong.MaxValue);
            }
            
            if (territory.Id == WaterID) territory.BuildingNodesCount = territory.MinimumSize <= 4 ? 4 : territory.MinimumSize;

            var didSpawnStronghold = false;
            var playerID = PlayerManager.Instance.GetIDWithColor(territory.PlayerColor);
            territory.PossibleNodeHexes.AddRange(territory.Hexes.Where(hex => (!hex.IsOnEdge && hex.NeighborHexes.Count >= 6)));

            for (var i = 0; i < territory.BuildingNodesCount; i++)
            {
                if (!territory.PossibleNodeHexes.Any())
                {
                    Debug.Log(
                        "Couldn't find a suitable place to spawn a building node for territory ID " + territory.Id);
                    break;
                }

                var randomHex = territory.PossibleNodeHexes[Random.Range(0, territory.PossibleNodeHexes.Count)];
                var spaceBetweenNodes = 5;
                
                if (didSpawnStronghold)
                {
                    playerID = ulong.MaxValue;
                    spaceBetweenNodes = 3;
                }

                if (territory.Id == WaterID)
                {
                    spaceBetweenNodes = 5;
                    Instantiate(Waypoint, randomHex.transform.position, Quaternion.identity);
                }
                else
                {
                    var settings = didSpawnStronghold || playerID == ulong.MaxValue ? TentSettings : StrongholdSettings;
                    var node = Instantiate(Node, randomHex.transform.position, Quaternion.identity);
                    node.NodeSettings = settings;
                    node.NetworkObject.SpawnWithOwnership(playerID);
                    territory.SpawnedNodes.Add(node.GetComponent<Node>());
                    didSpawnStronghold = true;
                }
                
                var hexesToRemove = GetExpandedHexes(randomHex, spaceBetweenNodes);
                foreach (var hex in hexesToRemove)
                {
                    territory.PossibleNodeHexes.Remove(hex);
                }

                territory.PossibleNodeHexes.Remove(randomHex);
            }
        }
    }

    private void UpdateMapColors()
    {
        var hexesToClassify = AllHexes.ToList();

        foreach (var territory in Territories)
        {
            if (territory.Id == WaterID)
            {
                foreach (var h in territory.Hexes)
                {
                    h.DistanceToWater = 0;
                    var materials = MapColors[0].Materials;
                    h.MeshRenderer.material = materials[Random.Range(0, materials.Length)];
                    hexesToClassify.Remove(h);
                }
            }
        }

        var level = 1;
        while (level < MapColors.Length)
        {
            foreach (var hex in hexesToClassify.ToList())
            {
                if (hex.NeighborHexes.Exists(neighbourHex => neighbourHex.DistanceToWater == level - 1))
                {
                    hex.DistanceToWater = level;
                    var materials = MapColors[level].Materials;
                    hex.MeshRenderer.material = materials[Random.Range(0, materials.Length)];
                    hexesToClassify.Remove(hex);
                }
            }

            level++;
        }

        foreach (var h in hexesToClassify)
        {
            var materials = MapColors[^1].Materials;
            h.MeshRenderer.material = materials[Random.Range(0, materials.Length)];
        }

        SpawnMapDetails();
    }

    private void SpawnMapDetails()
    {
        var detailsParent = new GameObject(name = "MapDetails");
        foreach (var territory in Territories)
        {
            var detailObjectCount = Random.Range(0, 4);
            if (territory.Id == WaterID) continue;
            for (var i = 0; i < detailObjectCount; i++)
            {
                if (territory.PossibleNodeHexes.Count < 1) break;
                
                var randomHex = territory.PossibleNodeHexes[Random.Range(0, territory.PossibleNodeHexes.Count)];
                var randomDetailObject = DetailPrefabs[Random.Range(0, DetailPrefabs.Length)];
                var randomRotation = Random.Range(0, 360);
                var randomSize = Random.Range(0.9f, 2f);
                var detail = Instantiate(randomDetailObject, detailsParent.transform);
                detail.localScale *= randomSize;
                detail.rotation = Quaternion.Euler(-90, randomRotation, 0);
                detail.position = randomHex.transform.position;
                territory.PossibleNodeHexes.Remove(randomHex);
                detail.SetParent(detailsParent.transform);
            }
        }

        CombineMeshes(detailsParent.transform, false);
        detailsParent.transform.position += new Vector3(0, 0.06f, 0);
    }

    private void GroupTerritories()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var minimapTerritories = new GameObject()
        {
            name = "MinimapTerritories",
            layer = LayerMask.NameToLayer("Minimap"),
            tag = "MinimapTerritories",
        };
        var water = new GameObject
        {
            name = "Water",
            layer = LayerMask.NameToLayer("Water"),
            tag = "Water",
        };
        var map = new GameObject
        {
            name = "Map",
            layer = LayerMask.NameToLayer("Map"),
        };

        foreach (var t in Territories)
        {
            if (t.Id == WaterID) continue;
            var minimapTerritory = new GameObject(name: "#MMT" + t.Id);
            minimapTerritory.transform.SetParent(minimapTerritories.transform);
            minimapTerritory.layer = LayerMask.NameToLayer("Minimap");
            var parent = minimapTerritory.transform;
            foreach (var hex in t.Hexes)
            {
                hex.transform.SetParent(parent);
            }
            CombineMeshes(parent, false, false);
        }
        
        
        foreach (var t in Territories)
        {
            var parent = (t.Id == WaterID) ? water.transform : map.transform;
    
            foreach (var hex in t.Hexes)
            {
                hex.transform.SetParent(parent);
            }
        }
        
        CombineMeshes(map.transform, true);
        CombineMeshes(water.transform, true);
        water.AddComponent<MeshCollider>();

        if (IsServer)
        {
            foreach (var territory in Territories)
            {
                if (territory.Id == WaterID) continue;

                var territoryComponent = Instantiate(TerritoryObject);
                var networkObject = territoryComponent.GetComponent<NetworkObject>();
                networkObject.AutoObjectParentSync = false;
                var nodes = territory.SpawnedNodes.ToList();
                territoryComponent.ID.Value = territory.Id;
                territoryComponent.Init(nodes);
                territory.TerritoryComponent = territoryComponent;
            }
            foreach (var territory in Territories.Where(territory => territory.Id != WaterID))
            {
                territory.TerritoryComponent.InitDict();
            }
            foreach (var t in Territories.Where(t => t.Id != WaterID))
            {
                t.TerritoryComponent.NetworkObject.SpawnWithOwnership(ulong.MaxValue);
            }
        }

        if (IsServer)
        {
            StartCoroutine(nameof(WaitForNavMeshBake));
            var mapLayers = new[] {"Map", "Water" };
            var waterLayers = new[] { "Water" };
            /*BuildNavmesh(map, mapLayers, "Walkable", 1);
            MapNavMeshSurface.UpdateNavMesh(MapNavMeshData);*/
            BuildNavmesh(water, waterLayers, "Water", 2);
        }
        
        water.transform.position += new Vector3(0, WaterLevel, 0);
        
        stopwatch.Stop();
        Debug.Log("Grouping Territories + Navmesh baking: " + stopwatch.ElapsedMilliseconds);
        OnGameFullyLoad.Raise();
    }
    private bool _isNavMeshUpdated;
    
    private void Awake()
    {
        NavMesh.onPreUpdate += OnNavMeshUpdated;
    }
    
    private void OnNavMeshUpdated()
    {
        _isNavMeshUpdated = true;
    }

    private IEnumerator WaitForNavMeshBake()
    {
         _isNavMeshUpdated = false;
        while (!_isNavMeshUpdated)
        {
            yield return null;
        }

        OnNavMeshFinishedBaking.Raise();
        NavMesh.onPreUpdate -= OnNavMeshUpdated;
    }


    private void BuildNavmesh(GameObject parent, string[] layerNames, string areaName, int agentIndex)
    {
        var navmeshSurface = parent.AddComponent<NavMeshSurface>();
        navmeshSurface.layerMask = LayerMask.GetMask(layerNames);
        navmeshSurface.defaultArea = NavMesh.GetAreaFromName(areaName);
        navmeshSurface.agentTypeID = NavMesh.GetSettingsByIndex(agentIndex).agentTypeID;
        navmeshSurface.BuildNavMesh();
    }

    private void CombineMeshes(Transform parent, bool multiMaterial, bool destroyAllChildren = true)
    {
        var meshCombiner = parent.AddComponent<MeshCombiner>();
        meshCombiner.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        meshCombiner.CreateMultiMaterialMesh = multiMaterial;
        meshCombiner.DeactivateCombinedChildren = false;
        meshCombiner.CombineInactiveChildren = true;
        meshCombiner.CombineMeshes(false);
        Destroy(meshCombiner);
        if (destroyAllChildren) DestroyAllChildren(parent.transform);
    }

    private void DestroyAllChildren(Transform parent)
    {
        while (parent.childCount > 0)
        {
            DestroyImmediate(parent.GetChild(0).gameObject);
        }
    }

    //Everyone loaded the hex grid, and we run this on the server side to sync the hexes...
    public void OnShare(Component sender, object data) 
    {
        foreach (var territory in Territories)
        {
            ShareTerritoriesClientRpc(territory.Id);
        }

        for (var i = 0; i < AllHexes.Count; i++)
        {
            var hex = AllHexes[i];
            
            if (hex.IsOnEdge)
            {
                ShareHexIDClientRpc((ushort)i, (ushort)hex.Id, (byte)hex.TerritoryIndex);
            }

        }

        FinishSharingRpc();

        GroupTerritories();
    }

    [Rpc(SendTo.NotServer)]
    private void ShareTerritoriesClientRpc(uint id)
    {
        var territory = new TerritoryData()
        {
            Id = id
        };
        Territories.Add(territory);
    }

    [Rpc(SendTo.NotServer)]
    private void ShareHexIDClientRpc(ushort index, ushort id, byte tIndex)
    {
        var hex = AllHexes[(int)index];
        hex.Id = id;

        UpdateTerritoryHexes(Territories[tIndex], hex, tIndex);
    }

    [Rpc(SendTo.NotServer)]
    private void FinishSharingRpc()
    {
        GenerateTerritories();
        UpdateMapColors();
        GroupTerritories();
    }
}