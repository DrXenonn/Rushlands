using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class UnitGroupManager : MonoBehaviour
{
    private readonly Dictionary<NavMeshAgent, Transform> _activeUnits = new();
    
    public void OnUnitGroupSpawned(Component sender, object data)
    {
        _activeUnits.Add((NavMeshAgent)sender, (Transform)data);
    }

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        InvokeRepeating(nameof(SlowUpdate), 0.2f, 0.2f);
    }

    private void SlowUpdate()
    {
        foreach (var activeUnit in _activeUnits.ToList())
        {
            if (!activeUnit.Key) 
            {
                _activeUnits.Remove(activeUnit.Key);
                continue;
            }
            
            activeUnit.Key.SetDestination(activeUnit.Value.position);
        }   
    }
}