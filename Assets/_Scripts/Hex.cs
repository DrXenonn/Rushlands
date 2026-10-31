using System.Collections.Generic;
using UnityEngine;
public class Hex : MonoBehaviour
{
    public uint Id;
    public bool IsOnEdge;
    public List<Hex> NeighborHexes = new();
    public int DistanceToWater = int.MaxValue;
    public MeshRenderer MeshRenderer;
    public int TerritoryIndex;

    private void OnValidate()
    {
        //InitializeNeighborHexes();
    }

    private void InitializeNeighborHexes()
    {
        NeighborHexes = new List<Hex>();

        var hexColliders = Physics.OverlapSphere(transform.position, 1.2f);
        foreach (var c in hexColliders)
        {
            var hex = c.GetComponent<Hex>();

            if (hex != null && hex != this)
            {
                NeighborHexes.Add(hex);
            }
        }

        // Sort neighbors by angle to ensure a fixed order
        NeighborHexes.Sort((a, b) => GetAngle(transform.position, a.transform.position)
            .CompareTo(GetAngle(transform.position, b.transform.position)));

        // Check if on the edge
        if (NeighborHexes.Count < 6)
        {
            IsOnEdge = true;
        }
    }

    /// <summary>
    /// Calculates the angle of a neighboring hex relative to the current hex.
    /// </summary>
    private float GetAngle(Vector3 center, Vector3 target)
    {
        Vector3 direction = target - center;
        return Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
    }
}