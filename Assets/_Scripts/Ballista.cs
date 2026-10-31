using System;
using System.Collections;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(2)]
public class Ballista : NetworkBehaviour
{
    [HideInInspector] public Node Node;
    [SerializeField] private GameEvent OnBallistaSpawned;
    [SerializeField] private Transform PivotPoint;
    [SerializeField] private Transform SpawnPoint;
    [SerializeField] private LayerMask UnitLayerMask;
    [SerializeField] private Bolt BoltPrefab;
    [SerializeField] private Animator Animator;
    [SerializeField] private SfxData BallistaShootSfx;
    private SoundFXManager _soundFXManager;
    private UnitGroup _targetGroup;
    private float _range;
    private short _damage;
    private float _fireDelay;
    private WaitForSeconds _wait;
    private readonly Collider[] _unitGroupsInRange = new Collider[20];

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        OnBallistaSpawned.Raise(Node, this);
    }

    private void Start()
    {
        _soundFXManager = SoundFXManager.Instance;
    }

    public void Init()
    {
        var settings = Node.RunTimeSettings;
        _range = settings.Range;
        _damage = settings.BaseDamage;
        _fireDelay = settings.GetAttackDelay();
        if (_fireDelay <= 0) return;
        _wait = new WaitForSeconds(_fireDelay);
        StartCoroutine(nameof(Shoot));
    }
    
    public void OnGameLoadedForEveryone(Component sender, object data)
    {
        if (IsServer)
        {
            UpdateNodeClientRpc(Node.NetworkObjectId);
        }
    }
    
    [Rpc(SendTo.NotServer)]
    private void UpdateNodeClientRpc(ulong nodeId)
    {
        StartCoroutine(SetTransform(nodeId));
    }

    private IEnumerator SetTransform(ulong nodeId)
    {
        yield return new WaitForSeconds(0.5f);
        Node = NetworkManager.SpawnManager.SpawnedObjects[nodeId].GetComponent<Node>();
        transform.SetParent(Node.Visuals.GetBallistaTransform());
        transform.localRotation = Quaternion.Euler(0, 0, 180);
        transform.localPosition = Vector3.zero;
    }
    
    private IEnumerator Shoot()
    {
        yield return new WaitForSeconds(0.1f);
        while (true)
        {
            _targetGroup = FindEnemyUnitGroup();
            
            if (_targetGroup != null)
            {
                var target = _targetGroup.transform.position;
                LookAtTarget(target);
                Animator.Play("Ballista");
                _soundFXManager.PlaySfx(BallistaShootSfx, transform.position);
                ShootVisualsClientRpc(target);
            }
            
            yield return _wait;
        }
    }

    [Rpc(SendTo.NotServer)]
    private void ShootVisualsClientRpc(Vector3 target)
    {
        LookAtTarget(target);
        _soundFXManager.PlaySfx(BallistaShootSfx, transform.position);
        Animator.Play("Ballista");
    }

    public void SpawnBolt()
    {
        if (!IsServer) return;
        if (_targetGroup != null)
        {
            var bolt = Instantiate(BoltPrefab, SpawnPoint.position, quaternion.identity);
            bolt.Init(_targetGroup, _damage);
            bolt.NetworkObject.Spawn();
        }
    }
    
    private void LookAtTarget(Vector3 target)
    {
        var dir = (target - PivotPoint.position).normalized;
        var zAngle = -Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        PivotPoint.rotation = Quaternion.Euler(-90, 0, zAngle);
    }

    [CanBeNull]
    private UnitGroup FindEnemyUnitGroup()
    {
        Array.Clear(_unitGroupsInRange, 0, _unitGroupsInRange.Length);

        var count = Physics.OverlapSphereNonAlloc(
            PivotPoint.position, _range, _unitGroupsInRange, UnitLayerMask, 
            QueryTriggerInteraction.Collide
        );

        var playerTeamMap = PlayerManager.Instance.PlayerIdTeamMap;
        var nodeTeam = playerTeamMap[Node.OwnerClientId];

        for (var i = 0; i < count; i++)
        {
            var unitGroup = _unitGroupsInRange[i]?.GetComponent<UnitGroup>();
            if (unitGroup == null) continue;

            var unitGroupID = unitGroup.GetOwnerId();
            var unitGroupTeam = playerTeamMap[unitGroupID];

            if (unitGroupTeam == nodeTeam && nodeTeam != 0) continue;

            if (unitGroupID == Node.OwnerClientId) continue;

            return unitGroup;
        }

        return null;
    }


    public override void OnDestroy()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}