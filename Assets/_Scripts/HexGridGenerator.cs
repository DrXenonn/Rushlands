using System;
using Unity.Netcode;
using UnityEngine;

public class HexGridGenerator : NetworkBehaviour
{
    [SerializeField] private GameObject HexPrefab;
    [SerializeField] private TerritoryGenerator TerritoryGenerator;
    [SerializeField] private GameEvent HexGridGenerated;
    [SerializeField] private int GridWidth = 10;
    [SerializeField] private int GridHeight = 10;
    [SerializeField] private float HexSize = 1.0f;
    [SerializeField] private float HeightOffset = 0;

    private float _xOffset;
    private float _zOffset;

    private void OnValidate()
    {
    }

    public void OnGameSceneLoaded(Component sender, object data)
    {
        //GenerateHexGrid();
        MapHandler.Instance.IncreaseMapGenCountRpc();

        if (IsServer)
        {
            var c = TerritoryGenerator.Generate();
            StartCoroutine(c);
        }
    }

    private void GenerateHexGrid()
    {
        _xOffset = HexSize * 0.75f;
        _zOffset = HexSize * Mathf.Sqrt(3) / 2;

        for (var x = 0; x < GridWidth; x++)
        {
            for (var y = 0; y < GridHeight; y++)
            {
                var xPos = x * _xOffset;
                var zPos = y * _zOffset;

                if (x % 2 == 1)
                {
                    zPos += _zOffset / 2;
                }

                var hexPosition = new Vector3(xPos, HeightOffset, zPos);
                var hexTile = Instantiate(HexPrefab, hexPosition, Quaternion.Euler(-90, 0 ,30), transform);
                hexTile.name = $"Hex_{x}_{y}";
            }
        }
        
        //HexGridGenerated.Raise();
        //MapHandler.Instance.IncreaseMapGenCountRpc();
    }
}