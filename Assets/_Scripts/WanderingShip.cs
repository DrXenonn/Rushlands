using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class WanderingShip : MonoBehaviour
{
    private NavMeshAgent _agent;
    private readonly List<Vector3> _flagPositions = new();
    private NavMeshPath _navMeshPath;

    private void Start()
    {
        MoveComponentsToRoot();
    }

    private void MoveComponentsToRoot()
    {
        var prevNavMesh = GetComponent<NavMeshAgent>();
        if (prevNavMesh == null) return;

        _navMeshPath = new NavMeshPath();

        var root = transform.root;
        var newAgent = root.gameObject.AddComponent<NavMeshAgent>();

        newAgent.agentTypeID = prevNavMesh.agentTypeID;
        newAgent.baseOffset = prevNavMesh.baseOffset;
        newAgent.speed = prevNavMesh.speed;
        newAgent.angularSpeed = prevNavMesh.angularSpeed;
        newAgent.acceleration = prevNavMesh.acceleration;
        newAgent.stoppingDistance = prevNavMesh.stoppingDistance;
        newAgent.autoBraking = prevNavMesh.autoBraking;
        newAgent.avoidancePriority = prevNavMesh.avoidancePriority;
        newAgent.obstacleAvoidanceType = prevNavMesh.obstacleAvoidanceType;
        newAgent.radius = prevNavMesh.radius;
        newAgent.height = prevNavMesh.height;
        newAgent.areaMask = prevNavMesh.areaMask;
        root.GetComponent<NetworkTransform>().enabled = true;
        
        Destroy(prevNavMesh);
        _agent = newAgent;
    }


    public void OnNavMeshFinishedBaking(Component sender, object data)
    {
        Invoke(nameof(Init), 1f);
    }

    private void Init()
    {
        FindFlags();
        
        if (_flagPositions.Count > 1)
        {
            InvokeRepeating(nameof(SlowUpdate), 0, 0.5f);
        }
    }

    private void FindFlags()
    {
        var waterAreaIndex = NavMesh.GetAreaFromName("Water");
        var waterMask = 1 << waterAreaIndex;
        
        _agent.speed += Random.Range(-0.4f, 0.4f);
        var flags = GameObject.FindGameObjectsWithTag("Flag");
        foreach (var flag in flags)
        {
            var flagPos = flag.transform.position;
            if (NavMesh.CalculatePath(transform.position, flagPos, waterMask, _navMeshPath) &&
                _navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                _flagPositions.Add(flagPos);
            }
        }
    }

    private Vector3 GetRandomPosition()
    {
        return _flagPositions.Count == 0 ? transform.position : _flagPositions[Random.Range(0, _flagPositions.Count)];
    }

    private int _stuckCount;

    private void SlowUpdate()
    {
        if (StuckCheck()) return;
        if (!(_agent.remainingDistance <= _agent.stoppingDistance)) return;
        _agent.SetDestination(GetRandomPosition());
    }

    private bool StuckCheck()
    {
        if (!(_agent.velocity.magnitude < 0.15f)) return false;
        _stuckCount++;
        if (_stuckCount <= 4) return false;
        _agent.SetDestination(GetRandomPosition());
        _stuckCount = 0;
        return true;
    }
}